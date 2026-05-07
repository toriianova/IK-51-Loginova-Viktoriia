using Microsoft.AspNetCore.Mvc;
using ІК_51_23_Логінова_В.Р_.Models.Local;
using ІК_51_23_Логінова_В.Р_.Services;

namespace ІК_51_23_Логінова_В.Р_.Controllers
{
    [ApiController]
    [Route("api/favorites")]
    public class FavoritesController : ControllerBase
    {
        private readonly FavoritesService _favoritesService;
        private readonly SpotifyApiService _spotify;
        private readonly ILogger<FavoritesController> _logger;
        public FavoritesController(
        FavoritesService favoritesService,
        SpotifyApiService spotify,
        ILogger<FavoritesController> logger)
        {
            _favoritesService = favoritesService;
            _spotify = spotify;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Getting all favorite tracks");
            var favorites = await _favoritesService.GetAll();
            return Ok(favorites);//повертаєм 200
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Getting favorite track by ID: {Id}", id); 
            var favorite = await _favoritesService.GetById(id);

            if (favorite == null)
            {
                _logger.LogWarning("Favorite track not found. ID: {Id}", id);
                return NotFound("Запис не знайдено");
            }

            return Ok(favorite);
        }

        [HttpPost("by-spotify-id")]//додати в обране за spotify-id
        public async Task<IActionResult> AddBySpotifyId([FromBody] AddBySpotifyIdRequest request)
        {
            _logger.LogInformation("Adding track to favorites. Spotify ID: {SpotifyId}", request?.SpotifyId);
            if (request == null || string.IsNullOrWhiteSpace(request.SpotifyId))
            {
                return BadRequest("SpotifyId обов'язковий");
            }

            var createdFavorite = await _favoritesService.AddBySpotifyId(request.SpotifyId, _spotify);

            if (createdFavorite == null)
            {
                _logger.LogWarning("Track not found in Spotify. Spotify ID: {SpotifyId}", request.SpotifyId);
                return NotFound("Трек не знайдено в Spotify");
            }
            _logger.LogInformation("Track added to favorites successfully");
            return CreatedAtAction(//201
                nameof(GetById),
                new { id = createdFavorite.Id },
                createdFavorite);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFavoriteRequest request)
        {
            _logger.LogInformation("Updating favorite track. ID: {Id}", id);
            if (request == null)
            {
                return BadRequest("Некоректні дані");
            }

            if (request.Rating < 0 || request.Rating > 10)
            {
                return BadRequest("Оцінка повинна бути в межах від 0 до 10");
            }

            var updatedFavorite = await _favoritesService.Update(id, request);

            if (updatedFavorite == null)
            {
                _logger.LogWarning("Favorite track for update not found. ID: {Id}", id);
                return NotFound("Запис не знайдено");
            }

            _logger.LogInformation("Favorite track updated successfully"); 
            return Ok(updatedFavorite);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Deleting favorite track. ID: {Id}", id); 
            var deleted = await _favoritesService.Delete(id);

            if (!deleted)
            {
                _logger.LogWarning("Favorite track for delete not found. ID: {Id}", id);
                return NotFound("Запис не знайдено");
            }
            _logger.LogInformation("Favorite track deleted successfully");
            return NoContent();//204
        }
    }
}