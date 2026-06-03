using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;
using backend.Infrastructure.Auth;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Mechanics.Api;

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
