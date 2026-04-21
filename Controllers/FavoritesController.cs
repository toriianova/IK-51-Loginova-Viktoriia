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

        public FavoritesController(FavoritesService favoritesService, SpotifyApiService spotify)
        {
            _favoritesService = favoritesService;
            _spotify = spotify;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var favorites = await _favoritesService.GetAll();
            return Ok(favorites);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var favorite = await _favoritesService.GetById(id);

            if (favorite == null)
            {
                return NotFound("Запис не знайдено");
            }

            return Ok(favorite);
        }

        [HttpPost("by-spotify-id")]
        public async Task<IActionResult> AddBySpotifyId([FromBody] AddBySpotifyIdRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SpotifyId))
            {
                return BadRequest("SpotifyId обов'язковий");
            }

            var createdFavorite = await _favoritesService.AddBySpotifyId(request.SpotifyId, _spotify);

            if (createdFavorite == null)
            {
                return NotFound("Трек не знайдено в Spotify");
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdFavorite.Id },
                createdFavorite);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFavoriteRequest request)
        {
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
                return NotFound("Запис не знайдено");
            }

            return Ok(updatedFavorite);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _favoritesService.Delete(id);

            if (!deleted)
            {
                return NotFound("Запис не знайдено");
            }

            return NoContent();
        }
    }
}