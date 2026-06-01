using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Modules.Play;
using backend.Modules.Changes;
using backend.Modules.Combat;
using backend.Services.Rpg;

namespace backend.Modules.Play;

public sealed class PlayStateService : IPlayStateService
{
    private readonly IGameStateService _gameStates;
    private readonly ICharacterService _characters;
    private readonly ITurnService _turns;
    private readonly IGameChangeService _changes;
    private readonly IMechanicRequestService _mechanicRequests;
    private readonly ICampaignMemoryService _memory;
    private readonly ICombatService _combat;
    private readonly ICharacterDomainService _characterDomain;

    public PlayStateService(
        IGameStateService gameStates,
        ICharacterService characters,
        ITurnService turns,
        IGameChangeService changes,
        IMechanicRequestService mechanicRequests,
        ICampaignMemoryService memory,
        ICombatService combat,
        ICharacterDomainService characterDomain)
    {
        _gameStates = gameStates;
        _characters = characters;
        _turns = turns;
        _changes = changes;
        _mechanicRequests = mechanicRequests;
        _memory = memory;
        _combat = combat;
        _characterDomain = characterDomain;
    }

    public async Task<RpgResult<PlayStateResponse>> BuildAsync(
        Guid accountId,
        Guid gameStateId,
        PlayStateBuildRequest request,
        CancellationToken cancellationToken)
    {
        var gameState = await _gameStates.GetGameStateAsync(accountId, gameStateId, cancellationToken);
        if (!gameState.HasValue)
        {
            return RpgResult<PlayStateResponse>.NotFound("GameState не найден.");
        }

        var characters = await _characters.GetCharactersAsync(accountId, gameStateId, cancellationToken);
        var recentTurns = await _turns.GetTurnsAsync(accountId, gameStateId, cancellationToken);
        var pendingChanges = await _changes.GetChangesAsync(accountId, gameStateId, "pending", cancellationToken);
        var mechanicRequests = await _mechanicRequests.GetRequestsAsync(accountId, gameStateId, "pending", cancellationToken);
        var memory = await _memory.GetMemoryAsync(accountId, gameStateId, cancellationToken);
        var combat = await _combat.GetCombatStateAsync(accountId, gameStateId, cancellationToken);

        var selectedCharacterId = request.CharacterId ?? GetFirstCharacterId(characters);
        var inventory = await GetInventoryAsync(accountId, gameStateId, selectedCharacterId, cancellationToken);
        var memoryValue = memory.Status == RpgResultStatus.Ok
            ? (JsonElement?)memory.Value
            : null;

        var changes = pendingChanges.Status == RpgResultStatus.Ok
            ? pendingChanges.Value ?? Array.Empty<JsonElement>()
            : Array.Empty<JsonElement>();
        var requests = mechanicRequests.Status == RpgResultStatus.Ok
            ? mechanicRequests.Value ?? Array.Empty<JsonElement>()
            : Array.Empty<JsonElement>();
        var turns = recentTurns.Status == RpgResultStatus.Ok
            ? recentTurns.Value ?? Array.Empty<JsonElement>()
            : Array.Empty<JsonElement>();
        var summary = request.ChangeSummary ?? PlayChangeApplicationSummary.Empty;

        var response = new PlayStateResponse(
            ResolveMode(request.PreferredMode, requests, changes, combat),
            request.MasterAnswer ?? GetLatestMasterAnswer(turns),
            gameState.Value,
            PlaySceneExtractor.Extract(memoryValue),
            characters,
            turns,
            changes,
            requests,
            combat,
            inventory,
            summary.Applied,
            summary.Skipped,
            summary.Failed,
            memoryValue,
            DateTimeOffset.UtcNow);

        return RpgResult<PlayStateResponse>.Ok(response);
    }

    private async Task<IReadOnlyList<JsonElement>> GetInventoryAsync(Guid accountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
    {
        if (!characterId.HasValue)
        {
            return Array.Empty<JsonElement>();
        }

        var inventory = await _characterDomain.GetInventoryAsync(accountId, gameStateId, characterId.Value, cancellationToken);
        return inventory.Status == RpgResultStatus.Ok
            ? inventory.Value ?? Array.Empty<JsonElement>()
            : Array.Empty<JsonElement>();
    }

    private static string ResolveMode(string? preferredMode, IReadOnlyList<JsonElement> mechanicRequests, IReadOnlyList<JsonElement> pendingChanges, JsonElement? combat)
    {
        if (mechanicRequests.Count > 0)
        {
            return "awaiting_roll";
        }

        if (IsActiveCombat(combat))
        {
            return "combat";
        }

        if (pendingChanges.Count > 0)
        {
            return "awaiting_change_confirmation";
        }

        return string.Equals(preferredMode, "travel", StringComparison.OrdinalIgnoreCase)
            ? "travel"
            : "narration";
    }

    private static bool IsActiveCombat(JsonElement? combat)
    {
        if (!combat.HasValue || combat.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (TryGetBool(combat.Value, "isActive", out var isActive)
            || TryGetBool(combat.Value, "активен", out isActive)
            || TryGetBool(combat.Value, "Р°РєС‚РёРІРµРЅ", out isActive))
        {
            return isActive;
        }

        return false;
    }

    private static bool TryGetBool(JsonElement source, string name, out bool value)
    {
        value = false;
        if (!source.TryGetProperty(name, out var element) || element.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            return false;
        }

        value = element.GetBoolean();
        return true;
    }

    private static Guid? GetFirstCharacterId(IReadOnlyList<JsonElement> characters)
    {
        foreach (var character in characters)
        {
            if (character.ValueKind == JsonValueKind.Object
                && character.TryGetProperty("id", out var id)
                && id.ValueKind == JsonValueKind.String
                && Guid.TryParse(id.GetString(), out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetLatestMasterAnswer(IReadOnlyList<JsonElement> turns)
    {
        foreach (var turn in turns)
        {
            if (turn.ValueKind == JsonValueKind.Object
                && turn.TryGetProperty("masterAnswer", out var answer)
                && answer.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(answer.GetString()))
            {
                return answer.GetString();
            }
        }

        return null;
    }
}
