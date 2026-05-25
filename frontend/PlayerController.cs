using Microsoft.AspNetCore.Mvc;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        [HttpGet]
        public ActionResult<Player> GetPlayer()
        {
            var player = new Player
            {
                Character = new Character
                {
                    Name = "Торвен Сталегрив",
                    Background = "Бывший солдат гарнизона",
                    Species = "Человек",
                    Class = "Воин",
                    Subclass = "Мастер боевых искусств",
                    Level = 1,
                    Experience = 0
                },

                Resources = new Resources
                {
                    Hp = new Stat
                    {
                        Max = 12,
                        Current = 12
                    },

                    Mana = new Stat
                    {
                        Max = 0,
                        Current = 0
                    },

                    ActionPoints = new Stat
                    {
                        Max = 1,
                        Current = 1
                    }
                },

                Attributes = new Attributes
                {
                    Strength = 16,
                    Dexterity = 13,
                    Constitution = 15,
                    Intelligence = 10,
                    Wisdom = 12,
                    Charisma = 8,
                    Initiative = 1,
                    Speed = 9,
                    Perception = 12
                },

                Inventory = new List<string>
                {
                    "Длинный меч",
                    "Щит",
                    "Кольчуга",
                    "Факел"
                }
            };

            return Ok(player);
        }
    }
}