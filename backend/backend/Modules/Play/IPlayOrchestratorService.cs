using backend.Contracts.Rpg.Common;
using backend.Modules.Play;

namespace backend.Modules.Play;

public interface IPlayOrchestratorService
{
    Task<RpgResult<PlayStateResponse>> ActAsync(
        Guid accountId,
        Guid gameStateId,
        PlayActRequest request,
        CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> ResolveAndContinueAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        PlayResolveAndContinueRequest request,
        CancellationToken cancellationToken);

    Task<RpgResult<PlayChangeApplicationSummary>> ApplySafeChangesAsync(
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken);
}
