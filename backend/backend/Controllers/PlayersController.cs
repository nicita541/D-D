using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// Compatibility controller for the old Players API.
    ///
    /// The database is now GameState-based, but this controller keeps the old
    /// /api/players endpoints working through PlayerDatabaseService.
    ///
    /// Later, when Auth + GameState API is ready, this controller can be replaced
    /// by GameStateController/AuthController.
    /// </summary>
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayerDatabaseService _players;

        public PlayersController(PlayerDatabaseService players)
        {
            _players = players;
        }

        /// <summary>
        /// Returns all players from active GameState documents.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<Player>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Player>>> GetPlayers()
        {
            var players = await _players.GetPlayersAsync();
            return Ok(players);
        }

        /// <summary>
        /// Returns one player by player UUID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Player), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Player>> GetPlayer(Guid id)
        {
            var player = await _players.GetPlayerByIdAsync(id);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "Персонаж не найден",
                    id
                });
            }

            return Ok(player);
        }

        /// <summary>
        /// Creates a new player.
        ///
        /// Under the hood this now creates a new GameState through
        /// game.create_new_game(...), then fills the default player.
        /// Until real auth is implemented, PlayerDatabaseService uses a local
        /// development account.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<object>> CreatePlayer([FromBody] Player player)
        {
            if (player == null)
            {
                return BadRequest(new
                {
                    message = "Тело запроса пустое или невалидное"
                });
            }

            if (string.IsNullOrWhiteSpace(player.Character.Name))
            {
                return BadRequest(new
                {
                    message = "Нужно указать имя персонажа: персонаж.имя"
                });
            }

            var id = await _players.CreatePlayerAsync(player);

            return CreatedAtAction(
                nameof(GetPlayer),
                new { id },
                new
                {
                    id,
                    message = "Персонаж создан. В новой БД он создан внутри GameState."
                }
            );
        }
    }
}
