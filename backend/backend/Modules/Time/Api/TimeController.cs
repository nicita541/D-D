using backend.Infrastructure.Auth;
using backend.Modules.Time.Application;
using backend.Modules.Time.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Time.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/time")]
public sealed class TimeController : ControllerBase
{
    private readonly ITimeService _time;
    private readonly ICurrentUserService _currentUser;

    public TimeController(ITimeService time, ICurrentUserService currentUser)
    {
        _time = time;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> GetTime(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _time.GetTimeAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpPost("advance")]
    public async Task<ActionResult> AdvanceTime(Guid gameStateId, [FromBody] AdvanceTimeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _time.AdvanceTimeAsync(current.AccountId, gameStateId, request ?? new AdvanceTimeRequest(), cancellationToken));
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
