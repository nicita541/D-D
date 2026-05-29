using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/checks")]
public sealed class ChecksController : ControllerBase
{
    private readonly IAbilityCheckService _checks;
    private readonly ICurrentUserService _currentUser;

    public ChecksController(IAbilityCheckService checks, ICurrentUserService currentUser)
    {
        _checks = checks;
        _currentUser = currentUser;
    }

    [HttpPost("ability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CreateAbilityCheck(Guid gameStateId, [FromBody] AbilityCheckRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _checks.CreateAbilityCheckAsync(current.AccountId, gameStateId, request ?? new AbilityCheckRequest(), cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetChecks(Guid gameStateId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _checks.GetChecksAsync(current.AccountId, gameStateId, limit, cancellationToken));
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
