using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayerDatabaseService _playerDatabaseService;

        public PlayersController(PlayerDatabaseService playerDatabaseService)
        {
            _playerDatabaseService = playerDatabaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Player>>> GetPlayers()
        {
            var players = await _playerDatabaseService.GetPlayersAsync();
            return Ok(players);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Player>> GetPlayer(Guid id)
        {
            var player = await _playerDatabaseService.GetPlayerByIdAsync(id);

            if (player == null)
            {
                return NotFound();
            }

            return Ok(player);
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreatePlayer([FromBody] Player player)
        {
            var playerId = await _playerDatabaseService.CreatePlayerAsync(player);

            return Ok(new
            {
                id = playerId,
                message = "Player created successfully"
            });
        }
    }
}