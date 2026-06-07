using backend.Infrastructure.Auth;
using backend.Modules.Invites.Application;
using backend.Modules.Invites.Contracts;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Modules.Invites.Api;

[Authorize]
[ApiController]
[Route("api")]
public sealed class InvitesController : ControllerBase
{
    private readonly IInviteService _invites;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public InvitesController(
        IInviteService invites,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _invites = invites;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpPost("game-states/{gameStateId:guid}/invites")]
    public async Task<ActionResult> CreateInvite(
        Guid gameStateId,
        [FromBody] CreateInviteRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден." });
        }

        if (!access.CanManageGame)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для invite." });
        }

        return ToActionResult(await _invites.CreateInviteAsync(
            current.AccountId,
            gameStateId,
            request ?? new CreateInviteRequest(),
            cancellationToken));
    }

    [AllowAnonymous]
    [EnableRateLimiting("invites")]
    [HttpGet("invites/{token}")]
    public async Task<ActionResult> PreviewInvite(string token, CancellationToken cancellationToken)
        => ToActionResult(await _invites.PreviewInviteAsync(token, cancellationToken));

    [EnableRateLimiting("invites")]
    [HttpPost("invites/{token}/accept")]
    public async Task<ActionResult> AcceptInvite(string token, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _invites.AcceptInviteAsync(current.AccountId, token, cancellationToken);
        if (result.Status == RpgResultStatus.Ok && result.Value is not null)
        {
            await _realtime.NotifyAsync(
                result.Value.GameStateId,
                GameRealtimeEvents.InviteAccepted,
                "invite accepted",
                cancellationToken);
            await _realtime.NotifyAsync(
                result.Value.GameStateId,
                GameRealtimeEvents.PartyUpdated,
                "party member joined",
                cancellationToken);
        }

        return ToActionResult(result);
    }

    [HttpDelete("game-states/{gameStateId:guid}/invites/{inviteId:guid}")]
    public async Task<ActionResult> RevokeInvite(Guid gameStateId, Guid inviteId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден." });
        }

        if (!access.CanManageGame)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для invite." });
        }

        var result = await _invites.RevokeInviteAsync(current.AccountId, gameStateId, inviteId, cancellationToken);
        return result.Status == RpgResultStatus.Ok
            ? Ok(new OperationResponse { Id = inviteId, Message = "Invite отозван." })
            : ToActionResult(result);
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный invite." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Invite не найден." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Invite недоступен." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Invite service недоступен." })
        };
}
