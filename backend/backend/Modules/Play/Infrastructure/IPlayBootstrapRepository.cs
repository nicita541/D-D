using backend.Contracts.Rpg.Play;

namespace backend.Repositories.Rpg;

public enum PlayBootstrapRepositoryStatus
{
    Ok,
    NotFound,
    NoCharacters,
    AlreadyBootstrapped
}

public sealed record PlayBootstrapRepositoryResult(
    PlayBootstrapRepositoryStatus Status,
    PlayBootstrapResponse? Response);

public interface IPlayBootstrapRepository
{
    Task<PlayBootstrapRepositoryResult> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
