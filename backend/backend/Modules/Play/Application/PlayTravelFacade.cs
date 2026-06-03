using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Travel.Application;
using backend.Modules.Travel.Infrastructure;

namespace backend.Modules.Play.Application;

public sealed class PlayTravelFacade : IPlayTravelFacade
{
    private readonly ITravelService _travel;
    private readonly IPlayStateService _playState;

    public PlayTravelFacade(ITravelService travel, IPlayStateService playState)
    {
        _travel = travel;
        _playState = playState;
    }

    public async Task<RpgResult<PlayStateResponse>> TravelAsync(
        Guid accountId,
        Guid gameStateId,
        PlayTravelRequest request,
        CancellationToken cancellationToken)
    {
        var move = await _travel.MoveAsync(accountId, gameStateId, request, cancellationToken);
        if (move.Status != RpgResultStatus.Ok)
        {
            return MapFailure(move);
        }

        return await _playState.BuildAsync(
            accountId,
            gameStateId,
            new PlayStateBuildRequest(PreferredMode: "travel"),
            cancellationToken);
    }

    public Task<RpgResult<PlayStateResponse>> MoveLocationAsync(
        Guid accountId,
        Guid gameStateId,
        PlayTravelRequest request,
        CancellationToken cancellationToken)
        => TravelAsync(accountId, gameStateId, request, cancellationToken);

    private static RpgResult<PlayStateResponse> MapFailure<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.NotFound => RpgResult<PlayStateResponse>.NotFound(result.Message ?? "GameState не найден."),
            RpgResultStatus.BadRequest => RpgResult<PlayStateResponse>.BadRequest(result.Message ?? "Перемещение некорректно."),
            RpgResultStatus.Conflict => RpgResult<PlayStateResponse>.Conflict(result.Message ?? "Перемещение конфликтует с текущим состоянием."),
            RpgResultStatus.ServiceUnavailable => RpgResult<PlayStateResponse>.ServiceUnavailable(null, result.Message ?? "Перемещение временно недоступно."),
            _ => RpgResult<PlayStateResponse>.BadRequest(result.Message ?? "Перемещение некорректно.")
        };
}
