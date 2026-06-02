using System.Text.Json;
using backend.Modules.Campaigns.Contracts;
using backend.Modules.Ai.Infrastructure;
using backend.Modules.Campaigns.Infrastructure;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.GameStates.Infrastructure;
using backend.Modules.Mechanics.Infrastructure;
using backend.Modules.Memory.Infrastructure;
using backend.Modules.Party.Infrastructure;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Story.Infrastructure;
using backend.Modules.Travel.Infrastructure;
using backend.Modules.Turns.Infrastructure;
using backend.Modules.World.Infrastructure;

namespace backend.Modules.Campaigns.Application;

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
