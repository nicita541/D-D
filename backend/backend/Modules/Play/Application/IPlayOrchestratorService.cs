using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Play.Application;

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
