using backend.Infrastructure.Auth;
using backend.Modules.Rest.Application;
using backend.Modules.Rest.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Rest.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/rest")]
public sealed class RestController : ControllerBase
{
    private readonly IRestService _rest;
    private readonly ICurrentUserService _currentUser;

    public RestController(IRestService rest, ICurrentUserService currentUser)
    {
        _rest = rest;
        _currentUser = currentUser;
    }

    [HttpPost("short")]
    public async Task<ActionResult> ShortRest(Guid gameStateId, [FromBody] RestRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _rest.ShortRestAsync(current.AccountId, gameStateId, request ?? new RestRequest(), cancellationToken));
    }

    [HttpPost("long")]
    public async Task<ActionResult> LongRest(Guid gameStateId, [FromBody] RestRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _rest.LongRestAsync(current.AccountId, gameStateId, request ?? new RestRequest(), cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт состояния." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };
}
