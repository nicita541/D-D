using backend.Shared.Contracts;
using backend.Shared.Kernel;
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

namespace backend.Modules.Travel.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/travel")]
public sealed class TravelController : ControllerBase
{
    private readonly ITravelService _travel;
    private readonly ICurrentUserService _currentUser;

    public TravelController(ITravelService travel, ICurrentUserService currentUser)
    {
        _travel = travel;
        _currentUser = currentUser;
    }

    [HttpGet("options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetOptions(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _travel.GetOptionsAsync(current.AccountId, gameStateId, cancellationToken);
        return result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "GameState не найден." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
