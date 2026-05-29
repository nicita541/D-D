using System.Text.Json;
using backend.Services.Rpg;

namespace backend.Repositories.Rpg;

public interface IAbilityCheckRepository
{
    Task<int?> GetAbilityScoreAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        string normalizedAbility,
        CancellationToken cancellationToken);

    Task<JsonElement?> CreateAbilityCheckAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        string ability,
        int abilityScore,
        int modifier,
        int difficultyClass,
        bool success,
        string reason,
        DiceRollResult roll,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetChecksAsync(
        Guid accountId,
        Guid gameStateId,
        int limit,
        CancellationToken cancellationToken);
}
