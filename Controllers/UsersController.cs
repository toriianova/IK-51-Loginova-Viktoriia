using Microsoft.AspNetCore.Mvc;
using ІК_51_23_Логінова_В.Р_.Services;

namespace ІК_51_23_Логінова_В.Р_.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly DatabaseService _database;

        public UsersController(DatabaseService database)
        {
            _database = database;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            long telegramId,
            string? username,
            string? firstName)
        {
            var user = await _database.RegisterUser(
                telegramId,
                username,
                firstName);

            return Ok(user);
        }

        [HttpGet("{telegramId}")]//повертає інфу про користувача
        public async Task<IActionResult> GetUser(long telegramId)
        {
            var user = await _database.GetUserByTelegramId(telegramId);

            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            return Ok(user);
        }
    }
}