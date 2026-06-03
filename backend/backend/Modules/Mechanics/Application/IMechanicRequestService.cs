using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;

namespace backend.Modules.Mechanics.Application;

public interface IMechanicRequestService
{
    Task<RpgResult<IReadOnlyList<JsonElement>>> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ResolveAbilityCheckAsync(Guid accountId, Guid gameStateId, Guid requestId, MechanicRequestResolveAbilityCheckRequest request, CancellationToken cancellationToken);
}
