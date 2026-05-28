using System.Text.Json;
using backend.Contracts.Rpg.Campaigns;

namespace backend.Services.Rpg;

public interface ICampaignService
{
    Task<IReadOnlyList<JsonElement>> GetCampaignTemplatesAsync(CancellationToken cancellationToken);
    Task<JsonElement?> GetCampaignTemplateAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateCampaignTemplateAsync(CreateCampaignTemplateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCampaignTemplateAsync(Guid id, CancellationToken cancellationToken);
}
