using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.GameStates;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states")]
public sealed class GameStatesController : ControllerBase
{
    private readonly IGameStateService _gameStates;
    private readonly ICurrentUserService _currentUser;

    public GameStatesController(IGameStateService gameStates, ICurrentUserService currentUser)
    {
        _gameStates = gameStates;
        _currentUser = currentUser;
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
    public async Task<ActionResult<OperationResponse>> CreateGameState([FromBody] CreateGameStateRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var id = await _gameStates.CreateGameStateAsync(current.AccountId, request?.Name, cancellationToken);
        return CreatedAtAction(nameof(GetGameState), new { gameStateId = id }, new OperationResponse { Id = id, Message = "GameState создан" });
    }

    [HttpDelete("{gameStateId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> DeleteGameState(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var deleted = await _gameStates.DeleteGameStateAsync(current.AccountId, gameStateId, cancellationToken);
        return deleted
            ? Ok(new OperationResponse { Id = gameStateId, Message = "GameState удалён" })
            : NotFound(new MessageResponse { Message = "GameState не найден" });
    }
}
