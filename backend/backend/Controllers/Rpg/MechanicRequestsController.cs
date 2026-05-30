using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/mechanic-requests")]
public sealed class MechanicRequestsController : ControllerBase
{
    private readonly IMechanicRequestService _mechanicRequests;
    private readonly ICurrentUserService _currentUser;

    public MechanicRequestsController(IMechanicRequestService mechanicRequests, ICurrentUserService currentUser)
    {
        _mechanicRequests = mechanicRequests;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRequests(Guid gameStateId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _mechanicRequests.GetRequestsAsync(current.AccountId, gameStateId, status, cancellationToken));
    }

    [HttpPost("{requestId:guid}/resolve/ability-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResolveAbilityCheck(
        Guid gameStateId,
        Guid requestId,
        [FromBody] MechanicRequestResolveAbilityCheckRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _mechanicRequests.ResolveAbilityCheckAsync(
            current.AccountId,
            gameStateId,
            requestId,
            request ?? new MechanicRequestResolveAbilityCheckRequest(),
            cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
