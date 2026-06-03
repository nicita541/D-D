using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Turns.Contracts;
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

namespace backend.Modules.Turns.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/turns")]
public sealed class TurnsController : ControllerBase
{
    private readonly ITurnService _turns;
    private readonly ICurrentUserService _currentUser;

    public TurnsController(ITurnService turns, ICurrentUserService currentUser)
    {
        _turns = turns;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> CreateTurn(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _turns.CreateTurnAsync(current.AccountId, gameStateId, request ?? new CreateTurnRequest(), cancellationToken);

        return result.Status switch
        {
            RpgResultStatus.Ok => StatusCode(StatusCodes.Status201Created, result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, result.Value),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetTurns(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _turns.GetTurnsAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpGet("{turnId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetTurn(Guid gameStateId, Guid turnId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _turns.GetTurnAsync(current.AccountId, gameStateId, turnId, cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, result.Value),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
