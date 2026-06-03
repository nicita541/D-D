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

namespace backend.Modules.Ai.Api;

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
