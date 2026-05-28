using System.Text.Json;

namespace backend.Repositories.Rpg;

public interface IAiMasterContextRepository
{
    Task<JsonElement?> GetContextAsync(Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken);
}
