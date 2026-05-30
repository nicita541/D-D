using System.Text.Json;
using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;

namespace backend.Services.Rpg;

public interface ICharacterProgressionService
{
    Task<RpgResult<JsonElement>> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, AddExperienceRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, LevelUpRequest request, CancellationToken cancellationToken);
}
