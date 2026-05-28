using System.Text.Json;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class AiMasterContextService : IAiMasterContextService
{
    private readonly IAiMasterContextRepository _repository;

    public AiMasterContextService(IAiMasterContextRepository repository)
    {
        _repository = repository;
    }

    public Task<JsonElement?> GetContextAsync(Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken)
        => _repository.GetContextAsync(gameStateId, recentEventsLimit <= 0 ? 10 : recentEventsLimit, cancellationToken);
}
