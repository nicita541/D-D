using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Play.Infrastructure;

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
