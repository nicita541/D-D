using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;

namespace backend.Services.Rpg;

public interface IMechanicRequestService
{
    Task<RpgResult<IReadOnlyList<JsonElement>>> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ResolveAbilityCheckAsync(Guid accountId, Guid gameStateId, Guid requestId, MechanicRequestResolveAbilityCheckRequest request, CancellationToken cancellationToken);
}
