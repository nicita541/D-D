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
    DateTimeOffset GeneratedAt);
