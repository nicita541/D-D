using System.Text.Json;
using backend.Modules.Conditions.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Conditions.Application;

public interface IConditionStateService
{
    Task<RpgResult<JsonElement>> TickConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, TickConditionsRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> KnockoutAsync(Guid accountId, Guid gameStateId, Guid characterId, KnockoutRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ReviveAsync(Guid accountId, Guid gameStateId, Guid characterId, ReviveRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetActiveConditionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetCharacterStatesAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
