using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Turns.Contracts;
using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;

namespace backend.Modules.Play.Application;

public sealed class PlayCombatFacade : IPlayCombatFacade
{
    private const string DefaultContinueMessage = """
        Продолжи сцену после последнего результата проверки или механического действия.
        Учти последние resolved mechanic requests, броски, pending/applied changes и текущее состояние игры.
        """;

    private readonly ICombatService _combat;
    private readonly ITurnService _turns;
    private readonly IPlayStateService _playState;
    private readonly IPlayOrchestratorService _orchestrator;

    public PlayCombatFacade(
        ICombatService combat,
        ITurnService turns,
        IPlayStateService playState,
        IPlayOrchestratorService orchestrator)
    {
        _combat = combat;
        _turns = turns;
        _playState = playState;
        _orchestrator = orchestrator;
    }

    public async Task<RpgResult<PlayStateResponse>> StartAsync(
        Guid accountId,
        Guid gameStateId,
        PlayCombatStartRequest request,
        CancellationToken cancellationToken)
    {
        var startRequest = new StartCombatRequest();
        var participants = request.ResolvedParticipants;
        if (participants.Count == 0)
        {
            return RpgResult<PlayStateResponse>.BadRequest(
                "Для play/combat/start передайте participants. Автодобавление партии будет включено только после безопасного маппинга персонажей.");
        }

        startRequest.Participants.AddRange(participants);
        try
        {
            var id = await _combat.StartCombatAsync(accountId, gameStateId, startRequest, cancellationToken);
            if (!id.HasValue)
            {
                return RpgResult<PlayStateResponse>.NotFound("GameState не найден.");
            }

            return await _playState.BuildAsync(
                accountId,
                gameStateId,
                new PlayStateBuildRequest(PreferredMode: "combat"),
                cancellationToken);
        }
        catch (CombatValidationException ex)
        {
            return RpgResult<PlayStateResponse>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<PlayStateResponse>> ActionAsync(
        Guid accountId,
        Guid gameStateId,
        PlayCombatActionRequest request,
        CancellationToken cancellationToken)
    {
        var action = request.ResolvedAction;
        if (!string.Equals(action, "attack", StringComparison.OrdinalIgnoreCase))
        {
            return RpgResult<PlayStateResponse>.BadRequest("В этом tranche play/combat/action поддерживает только action=attack.");
        }

        try
        {
            var result = await _combat.AttackAsync(accountId, gameStateId, request.Attack ?? new CombatAttackRequest(), cancellationToken);
            if (!result.HasValue)
            {
                return RpgResult<PlayStateResponse>.NotFound("Участники боя не найдены.");
            }

            return await _playState.BuildAsync(
                accountId,
                gameStateId,
                new PlayStateBuildRequest(PreferredMode: "combat"),
                cancellationToken);
        }
        catch (CombatValidationException ex)
        {
            return RpgResult<PlayStateResponse>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<PlayStateResponse>> EndAsync(
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        var ended = await _combat.EndCombatAsync(accountId, gameStateId, cancellationToken);
        if (!ended)
        {
            return RpgResult<PlayStateResponse>.NotFound("Активный бой не найден.");
        }

        return await _playState.BuildAsync(accountId, gameStateId, new PlayStateBuildRequest(), cancellationToken);
    }

    public async Task<RpgResult<PlayStateResponse>> ContinueAsync(
        Guid accountId,
        Guid gameStateId,
        PlayContinueRequest request,
        CancellationToken cancellationToken)
    {
        var message = BuildContinueMessage(request);
        var result = await _turns.CreateTurnAsync(
            accountId,
            gameStateId,
            new CreateTurnRequest { Message = $"Продолжи активный бой. {message}" },
            cancellationToken);
        if (result.Status != RpgResultStatus.Ok)
        {
            return MapTurnFailure(result);
        }

        var summary = await _orchestrator.ApplySafeChangesAsync(accountId, gameStateId, cancellationToken);
        if (summary.Status != RpgResultStatus.Ok)
        {
            return RpgResult<PlayStateResponse>.BadRequest(summary.Message ?? "Не удалось применить safe changes.");
        }

        return await _playState.BuildAsync(
            accountId,
            gameStateId,
            new PlayStateBuildRequest(
                PreferredMode: "combat",
                MasterAnswer: GetOptionalString(result.Value, "masterAnswer"),
                ChangeSummary: summary.Value),
            cancellationToken);
    }

    private static string BuildContinueMessage(PlayContinueRequest request)
    {
        var message = string.IsNullOrWhiteSpace(request.ResolvedPlayerMessage)
            ? DefaultContinueMessage
            : request.ResolvedPlayerMessage;

        return string.IsNullOrWhiteSpace(request.ResolvedNote)
            ? message
            : $"{message.Trim()}{Environment.NewLine}{Environment.NewLine}Дополнительная заметка игрока: {request.ResolvedNote}";
    }

    private static RpgResult<PlayStateResponse> MapTurnFailure(RpgResult<JsonElement> turn)
        => turn.Status switch
        {
            RpgResultStatus.NotFound => RpgResult<PlayStateResponse>.NotFound(turn.Message ?? "GameState не найден."),
            RpgResultStatus.BadRequest => RpgResult<PlayStateResponse>.BadRequest(turn.Message ?? "Ход некорректен."),
            RpgResultStatus.Conflict => RpgResult<PlayStateResponse>.Conflict(turn.Message ?? "В этой игре уже обрабатывается ход."),
            RpgResultStatus.ServiceUnavailable => RpgResult<PlayStateResponse>.ServiceUnavailable(null, turn.Message ?? "AI временно недоступен."),
            _ => RpgResult<PlayStateResponse>.ServiceUnavailable(null, turn.Message ?? "Не удалось создать ход.")
        };

    private static string? GetOptionalString(JsonElement? source, string name)
    {
        return source.HasValue
            && source.Value.ValueKind == JsonValueKind.Object
            && source.Value.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }
}
