using backend.Modules.AccountCharacters.Infrastructure;

namespace backend.Modules.AccountCharacters.Application;

public sealed class AccountCharacterSyncService : IAccountCharacterSyncService
{
    private readonly IAccountCharacterSyncRepository _repository;

    public AccountCharacterSyncService(IAccountCharacterSyncRepository repository)
    {
        _repository = repository;
    }

    public Task ImportLatestProfileIntoGameAsync(Guid ownerAccountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
        => _repository.ImportLatestProfileIntoGameAsync(ownerAccountId, gameStateId, characterId, cancellationToken);

    public Task ExportGameCharactersAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.ExportGameCharactersAsync(ownerAccountId, gameStateId, cancellationToken);
}
