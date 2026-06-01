using backend.Modules.Changes;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Changes;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/changes")]
public sealed class GameChangesController : ControllerBase
{
    private readonly IGameChangeService _changes;
    private readonly ICurrentUserService _currentUser;

    public GameChangesController(IGameChangeService changes, ICurrentUserService currentUser)
    {
        _changes = changes;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> GetChanges(Guid gameStateId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _changes.GetChangesAsync(current.AccountId, gameStateId, status, cancellationToken));
    }

    [HttpGet("{changeId:guid}")]
    public async Task<ActionResult> GetChange(Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _changes.GetChangeAsync(current.AccountId, gameStateId, changeId, cancellationToken));
    }

    [HttpPost("{changeId:guid}/apply")]
    public async Task<ActionResult> ApplyChange(Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _changes.ApplyChangeAsync(current.AccountId, gameStateId, changeId, cancellationToken));
    }

    [HttpPost("{changeId:guid}/reject")]
    public async Task<ActionResult> RejectChange(Guid gameStateId, Guid changeId, [FromBody] RejectGameChangeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _changes.RejectChangeAsync(current.AccountId, gameStateId, changeId, request?.ResolvedReason ?? "Rejected by user.", cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Service unavailable." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
