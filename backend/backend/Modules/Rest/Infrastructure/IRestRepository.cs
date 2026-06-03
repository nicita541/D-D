using System.Text.Json;

namespace backend.Modules.Rest.Infrastructure;

public interface IRestRepository
{
    Task<JsonElement?> RestAsync(Guid accountId, Guid gameStateId, Guid? characterId, bool isLongRest, int minutes, string reason, CancellationToken cancellationToken);
}
