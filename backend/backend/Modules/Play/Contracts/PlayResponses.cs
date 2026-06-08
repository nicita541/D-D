using System.Text.Json;

namespace backend.Modules.Play.Contracts;

public sealed record PlaySceneDto(
    string? Title,
    string? Summary,
    string? CurrentObjective,
    string? CurrentThreat,
    Guid? LocationId,
    IReadOnlyList<Guid> ActiveNpcIds,
    IReadOnlyList<Guid> ActiveQuestIds,
    DateTimeOffset? UpdatedAt);

public sealed record PlayBootstrapResponse(
    bool Bootstrapped,
    Guid? LocationId,
    Guid? QuestId,
    Guid? QuestStepId,
    Guid? NpcId,
    PlaySceneDto Scene);

public sealed record PlayChangeApplicationItem(
    Guid? Id,
    string Operation,
    object? Result,
    string? Reason,
    string? Message);

public sealed record PlayChangeApplicationSummary(
    IReadOnlyList<PlayChangeApplicationItem> Applied,
    IReadOnlyList<PlayChangeApplicationItem> Skipped,
    IReadOnlyList<PlayChangeApplicationItem> Failed)
{
    public static PlayChangeApplicationSummary Empty { get; } = new(
        Array.Empty<PlayChangeApplicationItem>(),
        Array.Empty<PlayChangeApplicationItem>(),
        Array.Empty<PlayChangeApplicationItem>());
}

public sealed record PlayStateResponse(
    string Mode,
    string? MasterAnswer,
    JsonElement? GameState,
    PlaySceneDto? Scene,
    IReadOnlyList<JsonElement> Characters,
    IReadOnlyList<JsonElement> RecentTurns,
    IReadOnlyList<JsonElement> PendingChanges,
    IReadOnlyList<JsonElement> MechanicRequests,
    JsonElement? Combat,
    IReadOnlyList<JsonElement> Inventory,
    IReadOnlyList<PlayChangeApplicationItem> AppliedChanges,
    IReadOnlyList<PlayChangeApplicationItem> SkippedChanges,
    IReadOnlyList<PlayChangeApplicationItem> FailedChanges,
    JsonElement? Memory,
    IReadOnlyList<JsonElement> Monsters,
    IReadOnlyList<JsonElement> Loot,
    IReadOnlyList<JsonElement> Rewards,
    JsonElement? Progression,
    JsonElement? Currency,
    JsonElement? Time,
    IReadOnlyList<JsonElement> ActiveConditions,
    JsonElement? RestAvailable,
    IReadOnlyList<JsonElement> CharacterStates,
    DateTimeOffset GeneratedAt,
    PlayPermissionsDto? Permissions = null,
    CurrentPartyMemberDto? CurrentPartyMember = null);

public sealed record PlayPermissionsDto(
    bool CanRead,
    bool CanPlay,
    bool CanManage,
    bool CanViewSecrets,
    bool CanControlSelectedCharacter);

public sealed record CurrentPartyMemberDto(
    Guid? Id,
    string Role,
    Guid? CharacterId,
    bool IsHost);
