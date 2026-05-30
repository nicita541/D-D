namespace backend.Contracts.Rpg.Play;

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
