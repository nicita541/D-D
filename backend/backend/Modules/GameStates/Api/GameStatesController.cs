using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.GameStates.Contracts;
using backend.Infrastructure.Auth;
using backend.Modules.Realtime;
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

namespace backend.Modules.GameStates.Api;

[Authorize]
[ApiController]
[Route("api/game-states")]
public sealed class GameStatesController : ControllerBase
{
    private readonly IGameStateService _gameStates;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService? _access;
    private readonly IGameRealtimeNotifier? _realtime;

    public GameStatesController(
        IGameStateService gameStates,
        ICurrentUserService currentUser,
        IGameAccessService? access = null,
        IGameRealtimeNotifier? realtime = null)
    {
        _gameStates = gameStates;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetGameStates(CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return Ok(await _gameStates.GetGameStatesAsync(current.AccountId, cancellationToken));
    }

    [HttpGet("{gameStateId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetGameState(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var gameState = await _gameStates.GetGameStateAsync(current.AccountId, gameStateId, cancellationToken);
        return gameState.HasValue ? Ok(gameState.Value) : NotFound(new MessageResponse { Message = "GameState не найден" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OperationResponse>> CreateGameState([FromBody] CreateGameStateRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var id = await _gameStates.CreateGameStateAsync(current.AccountId, request?.Name, cancellationToken);
        if (!id.HasValue)
        {
            return Unauthorized(new MessageResponse { Message = "Сессия устарела. Войдите заново." });
        }

        await NotifyAsync(id.Value, GameRealtimeEvents.GameUpdated, "game created", cancellationToken);
        return CreatedAtAction(nameof(GetGameState), new { gameStateId = id.Value }, new OperationResponse { Id = id.Value, Message = "GameState создан" });
    }

    [HttpDelete("{gameStateId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> DeleteGameState(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = _access is null ? null : await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (_access is not null && (access is null || !access.CanManageGame))
        {
            return access is null
                ? NotFound(new MessageResponse { Message = "GameState не найден" })
                : StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для удаления игры." });
        }

        var deleted = await _gameStates.DeleteGameStateAsync(access?.OwnerAccountId ?? current.AccountId, gameStateId, cancellationToken);
        return deleted
            ? Ok(new OperationResponse { Id = gameStateId, Message = "GameState удалён" })
            : NotFound(new MessageResponse { Message = "GameState не найден" });
    }

    private Task NotifyAsync(Guid gameStateId, string eventName, string reason, CancellationToken cancellationToken)
        => _realtime?.NotifyAsync(gameStateId, eventName, reason, cancellationToken) ?? Task.CompletedTask;
}
