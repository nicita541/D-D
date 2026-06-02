using System.Text.Json;
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

namespace backend.Modules.Ai.Application;

public sealed class AiMasterContextService : IAiMasterContextService
{
    private readonly IAiMasterContextRepository _repository;

    public AiMasterContextService(IAiMasterContextRepository repository)
    {
        _repository = repository;
    }

    public Task<JsonElement?> GetContextAsync(Guid accountId, Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken)
        => _repository.GetContextAsync(accountId, gameStateId, recentEventsLimit <= 0 ? 10 : recentEventsLimit, cancellationToken);
}
