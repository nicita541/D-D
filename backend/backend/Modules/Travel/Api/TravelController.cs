using backend.Infrastructure.Auth;
using backend.Modules.Travel.Application;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
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
    private readonly IGameAccessService _access;

    public TravelController(ITravelService travel, ICurrentUserService currentUser, IGameAccessService access)
    {
        _travel = travel;
        _currentUser = currentUser;
        _access = access;
    }

    [HttpGet("options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetOptions(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден." });
        }

        if (!access.CanReadGame)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для путешествий." });
        }

        var result = await _travel.GetOptionsAsync(access.OwnerAccountId, gameStateId, cancellationToken);
        return result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "GameState не найден." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
