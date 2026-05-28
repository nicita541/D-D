using System.Text.Json;
using backend.Contracts.Rpg.Characters;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _repository;

    public CharacterService(ICharacterRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetCharactersAsync(accountId, gameStateId, cancellationToken);

    public Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => _repository.GetCharacterAsync(accountId, gameStateId, characterId, cancellationToken);

    public Task<Guid?> CreateCharacterAsync(Guid accountId, Guid gameStateId, CreateCharacterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            request.Name = "Герой";
        }

        return _repository.CreateCharacterAsync(accountId, gameStateId, request, cancellationToken);
    }

    public Task<bool> UpdateCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken)
        => _repository.UpdateCharacterAsync(accountId, gameStateId, characterId, request, cancellationToken);

    public Task<bool> DeleteCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => _repository.DeleteCharacterAsync(accountId, gameStateId, characterId, cancellationToken);
}
