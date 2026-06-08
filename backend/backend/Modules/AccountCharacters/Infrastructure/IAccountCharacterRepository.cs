using System.Text.Json;
using backend.Modules.AccountCharacters.Contracts;

namespace backend.Modules.AccountCharacters.Infrastructure;

public interface IAccountCharacterRepository
{
    Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, CancellationToken cancellationToken);
    Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
    Task<JsonElement?> CreateCharacterAsync(Guid accountId, AccountCharacterDraft draft, CancellationToken cancellationToken);
    Task<JsonElement?> UpdateCharacterAsync(Guid accountId, Guid characterId, UpdateAccountCharacterRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
}
