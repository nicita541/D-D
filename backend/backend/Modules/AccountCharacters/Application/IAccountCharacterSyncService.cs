namespace backend.Modules.AccountCharacters.Application;

public interface IAccountCharacterSyncService
{
    Task ImportLatestProfileIntoGameAsync(Guid ownerAccountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken);
    Task ExportGameCharactersAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken);
}
