using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Play.Application;

public interface IPlayTravelFacade
{
    Task<RpgResult<PlayStateResponse>> TravelAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> MoveLocationAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken);
}
