using System.Text.Json;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;

namespace backend.Modules.Mechanics.Infrastructure;

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
