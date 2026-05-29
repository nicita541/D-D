using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Memory;

namespace backend.Services.Rpg;

public interface ICampaignMemoryService
{
    Task<RpgResult<JsonElement>> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken);
}
