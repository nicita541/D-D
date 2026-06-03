using System.Text.Json;

namespace backend.Modules.Changes.Infrastructure;

public interface IGameChangeRepository
{
    Task<IReadOnlyList<JsonElement>?> GetChangesAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken);

    Task<JsonElement?> GetChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken);

    Task<JsonElement?> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken);

    Task<JsonElement?> RejectChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, string reason, CancellationToken cancellationToken);
}
