using System.Text.Json;

namespace backend.Modules.Characters.Infrastructure;

public interface ICharacterProgressionRepository
{
    Task<JsonElement?> GetProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);

    Task<JsonElement?> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, int experience, string reason, CancellationToken cancellationToken);

    Task<JsonElement?> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, int? requestedNewLevel, int? hpMaxAdd, CancellationToken cancellationToken);
}
