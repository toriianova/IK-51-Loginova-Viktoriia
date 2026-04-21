using Microsoft.AspNetCore.Mvc;
using ІК_51_23_Логінова_В.Р_.Services;

namespace ІК_51_23_Логінова_В.Р_.Controllers
{
    [ApiController]
    [Route("api/tracks")]
    public class TracksController : ControllerBase
    {
        private readonly SpotifyApiService _spotify;

        public TracksController(SpotifyApiService spotify)
        {
            _spotify = spotify;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query, string? sortBy = null, string? nameFilter = null)
        {
            try
            {
                var result = await _spotify.SearchTracks(query, sortBy, nameFilter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _spotify.GetTrackById(id);

                if (result == null)
                {
                    return NotFound("Трек не знайдено");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("albums/search")]
        public async Task<IActionResult> SearchAlbums(string query)
        {
            try
            {
                var result = await _spotify.SearchAlbums(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopByYear(int year)
        {
            try
            {
                if (year < 1900 || year > DateTime.Now.Year)
                {
                    return BadRequest("Некоректний рік");
                }

                var result = await _spotify.GetTopTracksByYear(year);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations(string artist, string? genre = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(artist))
                {
                    return BadRequest("Потрібно вказати артиста");
                }

                var result = await _spotify.GetRecommendations(artist, genre);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }
    }
}