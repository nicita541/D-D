using System.Text.Json;

namespace backend.Modules.Ai.Infrastructure;

public interface IAiMasterContextRepository
{
    Task<JsonElement?> GetContextAsync(Guid accountId, Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken);
}
