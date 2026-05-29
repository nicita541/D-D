using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/rolls")]
public sealed class RollsController : ControllerBase
{
    private readonly IDiceRollService _rolls;
    private readonly ICurrentUserService _currentUser;

    public RollsController(IDiceRollService rolls, ICurrentUserService currentUser)
    {
        _rolls = rolls;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Roll(Guid gameStateId, [FromBody] RollDiceRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _rolls.RollAsync(current.AccountId, gameStateId, request ?? new RollDiceRequest(), cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRolls(Guid gameStateId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _rolls.GetRollsAsync(current.AccountId, gameStateId, limit, cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
