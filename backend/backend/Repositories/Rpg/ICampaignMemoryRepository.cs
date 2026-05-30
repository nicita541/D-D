using System.Text.Json;
using backend.Contracts.Rpg.Memory;

namespace backend.Repositories.Rpg;

public interface ICampaignMemoryRepository
{
    Task<JsonElement?> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken);

    Task<JsonElement?> ApplyMemoryPatchAsync(Guid accountId, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetRecentLogEntriesAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken);

    Task EnsureMemoryAsync(Guid gameStateId, CancellationToken cancellationToken);
}
