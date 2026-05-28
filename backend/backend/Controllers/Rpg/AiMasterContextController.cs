using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/ai-context")]
public sealed class AiMasterContextController : ControllerBase
{
    private readonly IAiMasterContextService _context;
    private readonly ICurrentUserService _currentUser;

    public AiMasterContextController(IAiMasterContextService context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetContext(Guid gameStateId, [FromQuery] int recentEventsLimit = 10, CancellationToken cancellationToken = default)
    {
        var current = _currentUser.GetRequiredUser();
        var context = await _context.GetContextAsync(current.AccountId, gameStateId, recentEventsLimit, cancellationToken);
        return context.HasValue ? Ok(context.Value) : NotFound(new MessageResponse { Message = "GameState не найден" });
    }
}
