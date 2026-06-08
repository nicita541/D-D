using backend.Infrastructure.Auth;
using backend.Modules.Realtime;
using backend.Modules.Snapshots.Application;
using backend.Modules.Snapshots.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Snapshots.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/snapshots")]
public sealed class SnapshotsController : ControllerBase
{
    private readonly ISnapshotService _snapshots;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public SnapshotsController(
        ISnapshotService snapshots,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _snapshots = snapshots;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> List(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanReadGame)
        {
            return AccessDenied(access);
        }

        return ToActionResult(await _snapshots.ListSnapshotsAsync(access.OwnerAccountId, gameStateId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SnapshotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Create(Guid gameStateId, [FromBody] CreateSnapshotRequest? request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var result = await _snapshots.CreateSnapshotAsync(
            access.OwnerAccountId,
            gameStateId,
            access.RequestAccountId,
            request?.ResolvedReason ?? "Ручной снимок.",
            cancellationToken);

        return result.Status == RpgResultStatus.Ok && result.Value is not null
            ? CreatedAtAction(nameof(List), new { gameStateId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{snapshotId:guid}/restore")]
    [ProducesResponseType(typeof(SnapshotRestoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Restore(Guid gameStateId, Guid snapshotId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var result = await _snapshots.RestoreSnapshotAsync(access.OwnerAccountId, gameStateId, snapshotId, access.RequestAccountId, cancellationToken);
        if (result.Status == RpgResultStatus.Ok)
        {
            await _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.GameUpdated, "snapshot restored", cancellationToken);
        }

        return ToActionResult(result);
    }

    private async Task<GameAccess?> GetAccessAsync(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
    }

    private ActionResult AccessDenied(GameAccess? access)
        => access is null
            ? NotFound(new MessageResponse { Message = "GameState не найден" })
            : StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для snapshots." });

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Snapshot не найден." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт snapshot." }),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный snapshot." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Snapshot service недоступен." })
        };
}
