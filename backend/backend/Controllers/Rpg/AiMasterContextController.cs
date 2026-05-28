using backend.Contracts.Rpg.Common;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[ApiController]
[Route("api/game-states/{gameStateId:guid}/ai-context")]
public sealed class AiMasterContextController : ControllerBase
{
    private readonly IAiMasterContextService _context;

    public AiMasterContextController(IAiMasterContextService context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetContext(Guid gameStateId, [FromQuery] int recentEventsLimit = 10, CancellationToken cancellationToken = default)
    {
        var context = await _context.GetContextAsync(gameStateId, recentEventsLimit, cancellationToken);
        return context.HasValue ? Ok(context.Value) : NotFound(new MessageResponse { Message = "GameState не найден" });
    }
}
