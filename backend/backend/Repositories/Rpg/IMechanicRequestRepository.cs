using System.Text.Json;
using backend.Contracts.Rpg.Mechanics;

namespace backend.Repositories.Rpg;

public interface IMechanicRequestRepository
{
    Task<IReadOnlyList<JsonElement>?> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken);

    Task<MechanicRequestRecord?> GetPendingAbilityCheckRequestAsync(Guid accountId, Guid gameStateId, Guid requestId, CancellationToken cancellationToken);

    Task<JsonElement?> ResolveAsync(Guid accountId, Guid gameStateId, Guid requestId, JsonElement result, CancellationToken cancellationToken);
}
