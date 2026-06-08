using System.Text.Json;
using backend.Modules.AccountCharacters.Contracts;
using backend.Modules.AccountCharacters.Infrastructure;

namespace backend.Modules.AccountCharacters.Application;

public sealed class AccountCharacterService : IAccountCharacterService
{
    private readonly IAccountCharacterRepository _repository;

    public AccountCharacterService(IAccountCharacterRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, CancellationToken cancellationToken)
        => _repository.GetCharactersAsync(accountId, cancellationToken);

    public Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken)
        => _repository.GetCharacterAsync(accountId, characterId, cancellationToken);

    public Task<JsonElement?> GenerateCharacterAsync(Guid accountId, GenerateAccountCharacterRequest request, CancellationToken cancellationToken)
        => _repository.CreateCharacterAsync(accountId, AccountCharacterGenerator.Generate(request), cancellationToken);

    public Task<JsonElement?> UpdateCharacterAsync(Guid accountId, Guid characterId, UpdateAccountCharacterRequest request, CancellationToken cancellationToken)
        => _repository.UpdateCharacterAsync(accountId, characterId, request, cancellationToken);

    public Task<bool> DeleteCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken)
        => _repository.DeleteCharacterAsync(accountId, characterId, cancellationToken);
}
