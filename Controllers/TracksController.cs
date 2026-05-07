using Microsoft.AspNetCore.Mvc;
using ІК_51_23_Логінова_В.Р_.Services;

namespace ІК_51_23_Логінова_В.Р_.Controllers
{
    [ApiController]
    [Route("api/tracks")]//початок адреси для методів
    public class TracksController : ControllerBase
    {
        private readonly SpotifyApiService _spotify;
        private readonly ILogger<TracksController> _logger;

        public TracksController (SpotifyApiService spotify, ILogger<TracksController> logger)
        {
            _spotify = spotify;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query, string? sortBy = null, string? nameFilter = null)
        {
            try
            {
                _logger.LogInformation("Track search started. Query: {Query}", query);
                var result = await _spotify.SearchTracks(query, sortBy, nameFilter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while searching tracks");
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                _logger.LogInformation("Getting track by ID: {Id}", id);
                var result = await _spotify.GetTrackById(id);

                if (result == null)
                {
                    return NotFound("Трек не знайдено");//404
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting track by ID");
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("albums/search")]
        public async Task<IActionResult> SearchAlbums(string query)
        {
            try
            {
                _logger.LogInformation("Album search started. Query: {Query}", query);
                var result = await _spotify.SearchAlbums(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while searching albums");
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopByYear(int year)
        {
            try
            {
                _logger.LogInformation("Getting top tracks for year: {Year}", year);
                if (year < 1900 || year > DateTime.Now.Year)
                {
                    return BadRequest("Некоректний рік");//400
                }

                var result = await _spotify.GetTopTracksByYear(year);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting top tracks");
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations(string artist, string? genre = null)
        {
            try
            {
                _logger.LogInformation("Getting recommendations for artist: {Artist}", artist);
                if (string.IsNullOrWhiteSpace(artist))
                {
                    return BadRequest("Потрібно вказати артиста");//400
                }

                var result = await _spotify.GetRecommendations(artist, genre);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting recommendations");
                return StatusCode(500, $"Помилка при зверненні до Spotify: {ex.Message}");
            }
        }
    }
}