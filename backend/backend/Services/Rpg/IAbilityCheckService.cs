using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;

namespace backend.Services.Rpg;

public interface IAbilityCheckService
{
    Task<RpgResult<JsonElement>> CreateAbilityCheckAsync(Guid accountId, Guid gameStateId, AbilityCheckRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetChecksAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken);
}
