using System.Text.Json;

namespace backend.Modules.Time.Infrastructure;

public interface ITimeRepository
{
    Task<JsonElement?> GetTimeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> AdvanceTimeAsync(Guid accountId, Guid gameStateId, int minutes, string reason, bool tickConditions, CancellationToken cancellationToken);
}
