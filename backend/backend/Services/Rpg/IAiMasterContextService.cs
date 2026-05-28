using System.Text.Json;

namespace backend.Services.Rpg;

public interface IAiMasterContextService
{
    Task<JsonElement?> GetContextAsync(Guid accountId, Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken);
}
