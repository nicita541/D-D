using System.Text.Json;
using backend.Contracts.Rpg.Memory;

namespace backend.Repositories.Rpg;

public interface ICampaignMemoryRepository
{
    Task<JsonElement?> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken);

    Task EnsureMemoryAsync(Guid gameStateId, CancellationToken cancellationToken);
}
