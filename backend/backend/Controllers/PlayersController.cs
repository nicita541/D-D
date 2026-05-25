using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayerDatabaseService _players;

        public PlayersController(PlayerDatabaseService players)
        {
            _players = players;
        }

        [HttpGet]
        public async Task<ActionResult<List<Player>>> GetPlayers()
        {
            return Ok(await _players.GetPlayersAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Player>> GetPlayer(Guid id)
        {
            var player = await _players.GetPlayerByIdAsync(id);

            if (player == null)
            {
                return NotFound();
            }

            return Ok(player);
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreatePlayer([FromBody] Player player)
        {
            var id = await _players.CreatePlayerAsync(player);

            return Ok(new
            {
                id,
                message = "Персонаж создан"
            });
        }
    }
}