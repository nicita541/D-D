namespace backend.Modules.AccountCharacters.Infrastructure;

public interface IAccountCharacterSyncRepository
{
    Task ImportLatestProfileIntoGameAsync(Guid ownerAccountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken);
    Task ExportGameCharactersAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken);
}
