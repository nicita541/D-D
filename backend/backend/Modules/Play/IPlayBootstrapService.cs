using backend.Contracts.Rpg.Common;
using backend.Modules.Play;

namespace backend.Modules.Play;

public interface IPlayBootstrapService
{
    Task<RpgResult<PlayBootstrapResponse>> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
