using System.Text.Json;
using backend.Contracts.Rpg.Story;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class StoryService : IStoryService
{
    private readonly IStoryRepository _repository;

    public StoryService(IStoryRepository repository)
    {
        _repository = repository;
    }

    public Task<JsonElement?> GetStoryStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetStoryStateAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid?> UpsertStoryStateAsync(Guid accountId, Guid gameStateId, CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken)
        => _repository.UpsertStoryStateAsync(accountId, gameStateId, request, cancellationToken);
}
