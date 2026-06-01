using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Contracts.Rpg.Memory;
using backend.Contracts.Rpg.Turns;
using backend.Modules.Changes;
using backend.Services.Rpg;

namespace backend.Modules.Play;

public sealed class PlayApplicationService : IPlayApplicationService
{
    private const string DefaultStartMessage = """
        Начни новую одиночную RPG-сцену для моего персонажа.

        Используй текущее состояние игры, персонажей, мира, локации, квестов и истории из контекста.
        Представь стартовую сцену, ближайшую цель, атмосферу, важную угрозу или NPC.
        Не создавай финал сразу.
        Дай игроку понятную ситуацию и 2-4 естественных варианта действия, но не ограничивай его только ими.
        Если нужна проверка навыка или характеристики — предложи её через поддерживаемый JSON changes.
        """;

    private const string DefaultContinueMessage = """
        Продолжи сцену после последнего результата проверки или механического действия.
        Учти последние resolved mechanic requests, броски, pending/applied changes и текущее состояние игры.
        """;

    private readonly IGameStateService _gameStates;
    private readonly ICharacterService _characters;
    private readonly ITurnService _turns;
    private readonly IGameChangeService _changes;
    private readonly IMechanicRequestService _mechanicRequests;
    private readonly ICampaignMemoryService _memory;
    private readonly IPlayBootstrapService _bootstrap;
    private readonly IPlayStateService _playState;
    private readonly IPlayOrchestratorService _orchestrator;

    public PlayApplicationService(
        IGameStateService gameStates,
        ICharacterService characters,
        ITurnService turns,
        IGameChangeService changes,
        IMechanicRequestService mechanicRequests,
        ICampaignMemoryService memory,
        IPlayBootstrapService bootstrap,
        IPlayStateService playState,
        IPlayOrchestratorService orchestrator)
    {
        _gameStates = gameStates;
        _characters = characters;
        _turns = turns;
        _changes = changes;
        _mechanicRequests = mechanicRequests;
        _memory = memory;
        _bootstrap = bootstrap;
        _playState = playState;
        _orchestrator = orchestrator;
    }

    public Task<RpgResult<PlayStateResponse>> StatusAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _playState.BuildAsync(accountId, gameStateId, new PlayStateBuildRequest(), cancellationToken);

    public Task<RpgResult<PlayStateResponse>> ActAsync(
        Guid accountId,
        Guid gameStateId,
        PlayActRequest request,
        CancellationToken cancellationToken)
        => _orchestrator.ActAsync(accountId, gameStateId, request, cancellationToken);

    public async Task<RpgResult<JsonElement>> StartAsync(
        Guid accountId,
        Guid gameStateId,
        CreateTurnRequest? request,
        CancellationToken cancellationToken)
    {
        var gameState = await _gameStates.GetGameStateAsync(accountId, gameStateId, cancellationToken);
        if (!gameState.HasValue)
        {
            return RpgResult<JsonElement>.NotFound("GameState не найден.");
        }

        var characters = await _characters.GetCharactersAsync(accountId, gameStateId, cancellationToken);
        if (characters.Count == 0)
        {
            return RpgResult<JsonElement>.BadRequest("Перед стартом сюжета нужно создать персонажа.");
        }

        var startRequest = new CreateTurnRequest
        {
            Message = string.IsNullOrWhiteSpace(request?.ResolvedPlayerMessage)
                ? DefaultStartMessage
                : request.ResolvedPlayerMessage
        };

        return await _turns.CreateTurnAsync(accountId, gameStateId, startRequest, cancellationToken);
    }

    public Task<RpgResult<JsonElement>> MessageAsync(
        Guid accountId,
        Guid gameStateId,
        CreateTurnRequest? request,
        CancellationToken cancellationToken)
        => _turns.CreateTurnAsync(accountId, gameStateId, request ?? new CreateTurnRequest(), cancellationToken);

    public Task<RpgResult<JsonElement>> ResolveMechanicRequestAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        MechanicRequestResolveAbilityCheckRequest? request,
        CancellationToken cancellationToken)
        => _mechanicRequests.ResolveAbilityCheckAsync(
            accountId,
            gameStateId,
            requestId,
            request ?? new MechanicRequestResolveAbilityCheckRequest(),
            cancellationToken);

    public Task<RpgResult<PlayStateResponse>> ResolveAndContinueAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        PlayResolveAndContinueRequest request,
        CancellationToken cancellationToken)
        => _orchestrator.ResolveAndContinueAsync(accountId, gameStateId, requestId, request, cancellationToken);

    public Task<RpgResult<JsonElement>> ContinueAsync(
        Guid accountId,
        Guid gameStateId,
        PlayContinueRequest? request,
        CancellationToken cancellationToken)
    {
        var message = BuildContinueMessage(request ?? new PlayContinueRequest());
        return _turns.CreateTurnAsync(
            accountId,
            gameStateId,
            new CreateTurnRequest { Message = message },
            cancellationToken);
    }

    public Task<RpgResult<JsonElement>> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
        => _changes.ApplyChangeAsync(accountId, gameStateId, changeId, cancellationToken);

    public Task<RpgResult<JsonElement>> RejectChangeAsync(
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        RejectGameChangeRequest? request,
        CancellationToken cancellationToken)
    {
        var reason = string.IsNullOrWhiteSpace(request?.ResolvedReason)
            ? "Rejected by player."
            : request.ResolvedReason;

        return _changes.RejectChangeAsync(accountId, gameStateId, changeId, reason, cancellationToken);
    }

    public Task<RpgResult<PlayChangeApplicationSummary>> ApplySafeChangesAsync(
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
        => _orchestrator.ApplySafeChangesAsync(accountId, gameStateId, cancellationToken);

    public Task<RpgResult<PlayBootstrapResponse>> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _bootstrap.BootstrapAsync(accountId, gameStateId, cancellationToken);

    public Task<RpgResult<JsonElement>> SummarizeAsync(
        Guid accountId,
        Guid gameStateId,
        CampaignMemorySummarizeRequest? request,
        CancellationToken cancellationToken)
        => _memory.SummarizeMemoryAsync(accountId, gameStateId, request ?? new CampaignMemorySummarizeRequest(), cancellationToken);

    private static string BuildContinueMessage(PlayContinueRequest request)
    {
        var message = string.IsNullOrWhiteSpace(request.ResolvedPlayerMessage)
            ? DefaultContinueMessage
            : request.ResolvedPlayerMessage;

        return string.IsNullOrWhiteSpace(request.ResolvedNote)
            ? message
            : $"{message.Trim()}{Environment.NewLine}{Environment.NewLine}Дополнительная заметка игрока: {request.ResolvedNote}";
    }
}
