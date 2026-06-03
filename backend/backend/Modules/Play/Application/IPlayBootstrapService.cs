using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Play.Application;

public interface IPlayBootstrapService
{
    Task<RpgResult<PlayBootstrapResponse>> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
