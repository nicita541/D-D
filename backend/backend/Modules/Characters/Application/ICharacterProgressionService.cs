using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Characters.Application;

public interface ICharacterProgressionService
{
    Task<RpgResult<JsonElement>> GetProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, AddExperienceRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, LevelUpRequest request, CancellationToken cancellationToken);
}
