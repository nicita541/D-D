using System.Text.Json;
using backend.Modules.Story.Contracts;
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

namespace backend.Modules.Story.Application;

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
