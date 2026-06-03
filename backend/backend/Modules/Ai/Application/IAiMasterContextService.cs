using System.Text.Json;

namespace backend.Modules.Ai.Application;

public interface IAiMasterContextService
{
    Task<JsonElement?> GetContextAsync(Guid accountId, Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken);
}
