using System.Text.Json;

namespace backend.Modules.Conditions.Infrastructure;

public interface IConditionStateRepository
{
    Task<JsonElement?> TickConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, int turns, CancellationToken cancellationToken);

    Task<JsonElement?> KnockoutAsync(Guid accountId, Guid gameStateId, Guid characterId, string reason, CancellationToken cancellationToken);

    Task<JsonElement?> ReviveAsync(Guid accountId, Guid gameStateId, Guid characterId, int hp, bool clearDead, string reason, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetActiveConditionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetCharacterStatesAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
