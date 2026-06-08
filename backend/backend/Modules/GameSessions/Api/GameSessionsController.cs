using backend.Infrastructure.Auth;
using backend.Modules.GameSessions.Application;
using backend.Modules.GameSessions.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.GameSessions.Api;

[Authorize]
[ApiController]
[Route("api/game-sessions")]
public sealed class GameSessionsController : ControllerBase
{
    private readonly IGameSessionService _sessions;
    private readonly ICurrentUserService _currentUser;

    public GameSessionsController(IGameSessionService sessions, ICurrentUserService currentUser)
    {
        _sessions = sessions;
        _currentUser = currentUser;
    }

    [HttpPost("start")]
    public async Task<ActionResult> Start([FromBody] StartGameSessionRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _sessions.StartAsync(current.AccountId, request ?? new StartGameSessionRequest(), cancellationToken);
        return this.ToActionResult(result);
    }
}
