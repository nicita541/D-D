using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;

namespace backend.Modules.Mechanics.Application;

public interface IAbilityCheckService
{
    Task<RpgResult<JsonElement>> CreateAbilityCheckAsync(Guid accountId, Guid gameStateId, AbilityCheckRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetChecksAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken);
}
