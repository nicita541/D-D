using System.Text.Json;
using backend.Contracts.Common;
using backend.Contracts.GameStates;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// GameState controller for the PostgreSQL GameState schema.
/// </summary>
[ApiController]
[Route("api/game-states")]
public class GameStatesController : ControllerBase
{
    private readonly IGameStateService _gameStates;

    public GameStatesController(IGameStateService gameStates)
    {
        _gameStates = gameStates;
    }

    /// <summary>
    /// Returns all active GameState documents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JsonElement>>> GetGameStates(CancellationToken cancellationToken)
    {
        var documents = await _gameStates.GetGameStatesAsync(cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Returns one full GameState document by game_state UUID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JsonElement>> GetGameState(
        Guid id,
        CancellationToken cancellationToken)
    {
        var gameState = await _gameStates.GetGameStateAsync(id, cancellationToken);

        if (gameState == null)
        {
            return NotFound(new MessageWithIdResponse
            {
                Message = "Сохранение GameState не найдено",
                Id = id
            });
        }

        return Ok(gameState.Value);
    }

    /// <summary>
    /// Returns only the player object from a GameState document.
    /// </summary>
    [HttpGet("{id:guid}/player")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JsonElement>> GetGameStatePlayer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var player = await _gameStates.GetGameStatePlayerAsync(id, cancellationToken);

        if (player == null)
        {
            return NotFound(new MessageWithIdResponse
            {
                Message = "Игрок в сохранении GameState не найден",
                Id = id
            });
        }

        return Ok(player.Value);
    }

    /// <summary>
    /// Creates a new GameState using game.create_new_game(...).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateGameState(
        [FromBody] CreateGameStateRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _gameStates.CreateGameStateAsync(request?.Name, cancellationToken);

        return CreatedAtAction(
            nameof(GetGameState),
            new { id = result.Id },
            new CreateGameStateResponse
            {
                Id = result.Id,
                Message = "GameState создан",
                AccountId = result.AccountId,
                SaveName = result.SaveName
            }
        );
    }

    /// <summary>
    /// Soft deletes a GameState by setting game.game_states.is_active = false.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeleteGameState(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _gameStates.DeleteGameStateAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new MessageWithIdResponse
            {
                Message = "Активное сохранение GameState не найдено",
                Id = id
            });
        }

        return Ok(new MessageWithIdResponse
        {
            Id = id,
            Message = "GameState удалён мягко: is_active = false"
        });
    }
}
