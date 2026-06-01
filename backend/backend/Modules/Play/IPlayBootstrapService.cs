using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Play;

namespace backend.Services.Rpg;

public interface IPlayBootstrapService
{
    Task<RpgResult<PlayBootstrapResponse>> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
