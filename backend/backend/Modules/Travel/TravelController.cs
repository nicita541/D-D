using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

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
