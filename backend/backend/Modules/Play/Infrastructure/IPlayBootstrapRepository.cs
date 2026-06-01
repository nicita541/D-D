using backend.Modules.Play;

namespace backend.Modules.Play;

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
