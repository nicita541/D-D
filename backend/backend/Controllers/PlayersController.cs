using Microsoft.AspNetCore.Mvc;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        [HttpGet("/id")]
        public ActionResult<Player> GetPlayers()
        {
           throw new NotImplementedException();
        }

    }
}