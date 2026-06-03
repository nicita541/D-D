using backend.Modules.Combat.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Play.Application;

public interface IPlayCombatFacade
{
    Task<RpgResult<PlayStateResponse>> StartAsync(Guid accountId, Guid gameStateId, PlayCombatStartRequest request, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> ActionAsync(Guid accountId, Guid gameStateId, PlayCombatActionRequest request, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> EndAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> ContinueAsync(Guid accountId, Guid gameStateId, PlayContinueRequest request, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> ResolveOutcomeAsync(Guid accountId, Guid gameStateId, PlayCombatResolveOutcomeRequest request, CancellationToken cancellationToken);
}
