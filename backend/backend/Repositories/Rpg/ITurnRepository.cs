using System.Text.Json;
using backend.Contracts.Rpg.Turns;

namespace backend.Repositories.Rpg;

public interface ITurnRepository
{
    Task<PendingTurn?> CreatePendingTurnAsync(Guid accountId, Guid gameStateId, string playerMessage, CancellationToken cancellationToken);

    Task<JsonElement?> CompleteTurnAsync(
        PendingTurn turn,
        string masterAnswer,
        string rawAiResponse,
        string aiModel,
        IReadOnlyList<GameChangeProposal> changes,
        CancellationToken cancellationToken);

    Task<JsonElement?> FailTurnAsync(
        PendingTurn turn,
        string errorMessage,
        string? rawAiResponse,
        string? aiModel,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken);
}
