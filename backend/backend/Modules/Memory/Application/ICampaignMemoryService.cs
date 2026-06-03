using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Memory.Contracts;

namespace backend.Modules.Memory.Application;

public interface ICampaignMemoryService
{
    Task<RpgResult<JsonElement>> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> SummarizeMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemorySummarizeRequest request, CancellationToken cancellationToken);
}
