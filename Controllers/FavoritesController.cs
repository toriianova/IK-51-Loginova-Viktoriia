using Microsoft.AspNetCore.Mvc;
using ІК_51_23_Логінова_В.Р_.Models.Local;
using ІК_51_23_Логінова_В.Р_.Services;

namespace ІК_51_23_Логінова_В.Р_.Controllers
{
    [ApiController]
    [Route("api/favorites")]
    public class FavoritesController : ControllerBase
    {
        private readonly DatabaseService _database;
        private readonly SpotifyApiService _spotify;
        private readonly ILogger<FavoritesController> _logger;
        public FavoritesController(
            DatabaseService database,
            SpotifyApiService spotify,
            ILogger<FavoritesController> logger)
        {
            _database = database;
            _spotify = spotify;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long telegramId)
        {
            _logger.LogInformation("Getting favorite tracks for Telegram user: {TelegramId}", telegramId);

            var favorites = await _database.GetFavoritesByTelegramId(telegramId);

            if (favorites == null)
            {
                return Unauthorized("Користувач не зареєстрований");
            }

            return Ok(favorites);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long telegramId, Guid id)
        {
            _logger.LogInformation(
                "Getting favorite track by ID. Telegram user: {TelegramId}, ID: {Id}",
                telegramId,
                id);

            var favorite = await _database.GetFavoriteById(telegramId, id);

            if (favorite == null)
            {
                return NotFound("Користувач не зареєстрований або запис не знайдено");
            }

            return Ok(favorite);
        }

        [HttpPost("by-spotify-id")]
        public async Task<IActionResult> AddBySpotifyId(long telegramId, [FromBody] AddBySpotifyIdRequest request)
        {
            _logger.LogInformation("Adding track to favorites. Telegram user: {TelegramId}, Spotify ID: {SpotifyId}",
                telegramId, request?.SpotifyId);

            if (request == null || string.IsNullOrWhiteSpace(request.SpotifyId))
            {
                return BadRequest("SpotifyId обов'язковий");
            }

            var createdFavorite = await _database.AddFavoriteBySpotifyId(
                telegramId,
                request.SpotifyId,
                _spotify);

            if (createdFavorite == null)
            {
                return Unauthorized("Користувач не зареєстрований або трек не знайдено");
            }

            return Ok(createdFavorite);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long telegramId, Guid id, [FromBody] UpdateFavoriteRequest request)
        {
            _logger.LogInformation("Updating favorite track. Telegram user: {TelegramId}, ID: {Id}", telegramId, id);

            if (request == null)
            {
                return BadRequest("Некоректні дані");
            }

            if (request.Rating < 0 || request.Rating > 10)
            {
                return BadRequest("Оцінка повинна бути в межах від 0 до 10");
            }

            var updatedFavorite = await _database.UpdateFavorite(
                telegramId,
                id,
                request.Rating,
                request.Comment);

            if (updatedFavorite == null)
            {
                return NotFound("Користувач не зареєстрований або запис не знайдено");
            }

            return Ok(updatedFavorite);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long telegramId, Guid id)
        {
            _logger.LogInformation("Deleting favorite track. Telegram user: {TelegramId}, ID: {Id}", telegramId, id);

            var deleted = await _database.DeleteFavorite(telegramId, id);

            if (!deleted)
            {
                return NotFound("Користувач не зареєстрований або запис не знайдено");
            }

            return NoContent();
        }
    }
}