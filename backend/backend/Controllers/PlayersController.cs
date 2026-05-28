using backend.Contracts.Common;
using backend.Contracts.Players;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// Compatibility controller for the old Players API.
/// </summary>
[ApiController]
[Route("api/players")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _players;

    public PlayersController(IPlayerService players)
    {
        _players = players;
    }

    /// <summary>
    /// Returns all players from active GameState documents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Player>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Player>>> GetPlayers(CancellationToken cancellationToken)
    {
        var players = await _players.GetPlayersAsync(cancellationToken);
        return Ok(players);
    }

    /// <summary>
    /// Returns one player by player UUID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Player), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Player>> GetPlayer(Guid id, CancellationToken cancellationToken)
    {
        var player = await _players.GetPlayerByIdAsync(id, cancellationToken);

        if (player == null)
        {
            return NotFound(new MessageWithIdResponse
            {
                Message = "Персонаж не найден",
                Id = id
            });
        }

        return Ok(player);
    }

    /// <summary>
    /// Creates a new player through a GameState-backed compatibility flow.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreatePlayer(
        [FromBody] Player? player,
        CancellationToken cancellationToken)
    {
        if (player == null)
        {
            return BadRequest(new MessageResponse
            {
                Message = "Тело запроса пустое или невалидное"
            });
        }

        if (string.IsNullOrWhiteSpace(player.Character.Name))
        {
            return BadRequest(new MessageResponse
            {
                Message = "Нужно указать имя персонажа: персонаж.имя"
            });
        }

        var id = await _players.CreatePlayerAsync(player, cancellationToken);

        return CreatedAtAction(
            nameof(GetPlayer),
            new { id },
            new CreatePlayerResponse
            {
                Id = id,
                Message = "Персонаж создан. В новой БД он создан внутри GameState."
            }
        );
    }
}
