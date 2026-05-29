using System.Text.Json;

namespace backend.Repositories.Rpg;

public interface ICharacterProgressionRepository
{
    Task<JsonElement?> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, int experience, string reason, CancellationToken cancellationToken);

    Task<JsonElement?> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, int newLevel, int hpMaxAdd, CancellationToken cancellationToken);
}
