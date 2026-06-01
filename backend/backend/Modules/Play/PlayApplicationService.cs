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
        РќР°С‡РЅРё РЅРѕРІСѓСЋ РѕРґРёРЅРѕС‡РЅСѓСЋ RPG-СЃС†РµРЅСѓ РґР»СЏ РјРѕРµРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°.

        РСЃРїРѕР»СЊР·СѓР№ С‚РµРєСѓС‰РµРµ СЃРѕСЃС‚РѕСЏРЅРёРµ РёРіСЂС‹, РїРµСЂСЃРѕРЅР°Р¶РµР№, РјРёСЂР°, Р»РѕРєР°С†РёРё, РєРІРµСЃС‚РѕРІ Рё РёСЃС‚РѕСЂРёРё РёР· РєРѕРЅС‚РµРєСЃС‚Р°.
        РџСЂРµРґСЃС‚Р°РІСЊ СЃС‚Р°СЂС‚РѕРІСѓСЋ СЃС†РµРЅСѓ, Р±Р»РёР¶Р°Р№С€СѓСЋ С†РµР»СЊ, Р°С‚РјРѕСЃС„РµСЂСѓ, РІР°Р¶РЅСѓСЋ СѓРіСЂРѕР·Сѓ РёР»Рё NPC.
        РќРµ СЃРѕР·РґР°РІР°Р№ С„РёРЅР°Р» СЃСЂР°Р·Сѓ.
        Р”Р°Р№ РёРіСЂРѕРєСѓ РїРѕРЅСЏС‚РЅСѓСЋ СЃРёС‚СѓР°С†РёСЋ Рё 2-4 РµСЃС‚РµСЃС‚РІРµРЅРЅС‹С… РІР°СЂРёР°РЅС‚Р° РґРµР№СЃС‚РІРёСЏ, РЅРѕ РЅРµ РѕРіСЂР°РЅРёС‡РёРІР°Р№ РµРіРѕ С‚РѕР»СЊРєРѕ РёРјРё.
        Р•СЃР»Рё РЅСѓР¶РЅР° РїСЂРѕРІРµСЂРєР° РЅР°РІС‹РєР° РёР»Рё С…Р°СЂР°РєС‚РµСЂРёСЃС‚РёРєРё вЂ” РїСЂРµРґР»РѕР¶Рё РµС‘ С‡РµСЂРµР· РїРѕРґРґРµСЂР¶РёРІР°РµРјС‹Р№ JSON changes.
        """;

    private const string DefaultContinueMessage = """
        РџСЂРѕРґРѕР»Р¶Рё СЃС†РµРЅСѓ РїРѕСЃР»Рµ РїРѕСЃР»РµРґРЅРµРіРѕ СЂРµР·СѓР»СЊС‚Р°С‚Р° РїСЂРѕРІРµСЂРєРё РёР»Рё РјРµС…Р°РЅРёС‡РµСЃРєРѕРіРѕ РґРµР№СЃС‚РІРёСЏ.
        РЈС‡С‚Рё РїРѕСЃР»РµРґРЅРёРµ resolved mechanic requests, Р±СЂРѕСЃРєРё, pending/applied changes Рё С‚РµРєСѓС‰РµРµ СЃРѕСЃС‚РѕСЏРЅРёРµ РёРіСЂС‹.
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
            return RpgResult<JsonElement>.NotFound("GameState РЅРµ РЅР°Р№РґРµРЅ.");
        }

        var characters = await _characters.GetCharactersAsync(accountId, gameStateId, cancellationToken);
        if (characters.Count == 0)
        {
            return RpgResult<JsonElement>.BadRequest("РџРµСЂРµРґ СЃС‚Р°СЂС‚РѕРј СЃСЋР¶РµС‚Р° РЅСѓР¶РЅРѕ СЃРѕР·РґР°С‚СЊ РїРµСЂСЃРѕРЅР°Р¶Р°.");
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
            : $"{message.Trim()}{Environment.NewLine}{Environment.NewLine}Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ Р·Р°РјРµС‚РєР° РёРіСЂРѕРєР°: {request.ResolvedNote}";
    }
}
