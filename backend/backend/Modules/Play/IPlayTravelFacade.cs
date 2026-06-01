using backend.Contracts.Rpg.Common;

namespace backend.Modules.Play;

public interface IPlayTravelFacade
{
    Task<RpgResult<PlayStateResponse>> TravelAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> MoveLocationAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken);
}
