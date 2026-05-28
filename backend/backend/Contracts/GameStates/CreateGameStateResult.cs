namespace backend.Contracts.GameStates;

public sealed class CreateGameStateResult
{
    public required Guid Id { get; init; }

    public required Guid AccountId { get; init; }

    public required string SaveName { get; init; }
}
