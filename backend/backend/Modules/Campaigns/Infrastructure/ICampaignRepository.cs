using System.Text.Json;
using backend.Modules.Campaigns.Contracts;

namespace backend.Modules.Campaigns.Infrastructure;

public interface ICampaignRepository
{
    Task<IReadOnlyList<JsonElement>> GetCampaignTemplatesAsync(CancellationToken cancellationToken);
    Task<JsonElement?> GetCampaignTemplateAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateCampaignTemplateAsync(CreateCampaignTemplateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCampaignTemplateAsync(Guid id, CancellationToken cancellationToken);
}
