using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Memory;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/memory")]
public sealed class CampaignMemoryController : ControllerBase
{
    private readonly ICampaignMemoryService _memory;
    private readonly ICurrentUserService _currentUser;

    public CampaignMemoryController(ICampaignMemoryService memory, ICurrentUserService currentUser)
    {
        _memory = memory;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetMemory(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _memory.GetMemoryAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateMemory(Guid gameStateId, [FromBody] CampaignMemoryRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _memory.UpdateMemoryAsync(current.AccountId, gameStateId, request ?? new CampaignMemoryRequest(), cancellationToken));
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
