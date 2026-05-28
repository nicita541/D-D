using System.Text.Json;
using backend.Contracts.Rpg.Campaigns;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _repository;

    public CampaignService(ICampaignRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<JsonElement>> GetCampaignTemplatesAsync(CancellationToken cancellationToken)
        => _repository.GetCampaignTemplatesAsync(cancellationToken);

    public Task<JsonElement?> GetCampaignTemplateAsync(Guid id, CancellationToken cancellationToken)
        => _repository.GetCampaignTemplateAsync(id, cancellationToken);

    public Task<Guid> CreateCampaignTemplateAsync(CreateCampaignTemplateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Название кампании обязательно.", nameof(request));
        }

        return _repository.CreateCampaignTemplateAsync(request, cancellationToken);
    }

    public Task<bool> DeleteCampaignTemplateAsync(Guid id, CancellationToken cancellationToken)
        => _repository.DeleteCampaignTemplateAsync(id, cancellationToken);
}
