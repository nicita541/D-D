using System.Text.Json;
using backend.Modules.Characters.Contracts;

namespace backend.Modules.Characters.Application;

public interface ICharacterService
{
    Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);

    Task<Guid?> CreateCharacterAsync(Guid accountId, Guid gameStateId, CreateCharacterRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
}
