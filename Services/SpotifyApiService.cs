using System.Net.Http.Headers;
using System.Text.Json;
using ІК_51_23_Логінова_В.Р_.Models.Spotify;

namespace ІК_51_23_Логінова_В.Р_.Services
{
    public class SpotifyApiService
    {
        private readonly SpotifyAuthService _auth;
        private readonly HttpClient _http;

        public SpotifyApiService(SpotifyAuthService auth)
        {
            _auth = auth;
            _http = new HttpClient();
        }

        public async Task<List<TrackResult>> SearchTracks(string query, string? sortBy = null, string? nameFilter = null)
        {
            var token = await _auth.GetAccessToken();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=10";

            var responseMessage = await _http.GetAsync(url);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {responseMessage.StatusCode}");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<SpotifySearchResponse>(json, options);

            if (response?.Tracks?.Items == null)
            {
                return new List<TrackResult>();
            }

            var result = response.Tracks.Items.Select(track => new TrackResult
            {
                Id = track.Id,
                Name = track.Name,
                Artist = track.Artists != null && track.Artists.Count > 0
                    ? string.Join(", ", track.Artists.Select(a => a.Name))
                    : "Невідомий виконавець",
                Album = track.Album?.Name ?? "Невідомий альбом"
            }).ToList();

            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                result = result
                    .Where(t => t.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            result = sortBy?.ToLower() switch
            {
                "name" => result.OrderBy(t => t.Name).ToList(),
                "artist" => result.OrderBy(t => t.Artist).ToList(),
                _ => result
            };

            return result;
        }

        public async Task<TrackDetails?> GetTrackById(string id)
        {
            var token = await _auth.GetAccessToken();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"https://api.spotify.com/v1/tracks/{id}";

            var responseMessage = await _http.GetAsync(url);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {responseMessage.StatusCode}");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var track = JsonSerializer.Deserialize<SpotifyTrackDetailsResponse>(json, options);

            if (track == null)
            {
                return null;
            }

            return new TrackDetails
            {
                Id = track.Id,
                Name = track.Name,
                Artist = track.Artists != null && track.Artists.Count > 0
                    ? string.Join(", ", track.Artists.Select(a => a.Name))
                    : "Невідомий виконавець",
                Album = track.Album?.Name ?? "Невідомий альбом",
                DurationMs = track.DurationMs,
                Explicit = track.Explicit,
            };
        }

        public async Task<List<AlbumResult>> SearchAlbums(string query)
        {
            var token = await _auth.GetAccessToken();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=album&limit=10";

            var responseMessage = await _http.GetAsync(url);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {responseMessage.StatusCode}");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<SpotifyAlbumSearchResponse>(json, options);

            if (response?.Albums?.Items == null)
            {
                return new List<AlbumResult>();
            }

            return response.Albums.Items.Select(album => new AlbumResult
            {
                Id = album.Id,
                Name = album.Name,
                Artist = album.Artists != null && album.Artists.Count > 0
                    ? string.Join(", ", album.Artists.Select(a => a.Name))
                    : "Невідомий виконавець",
                ReleaseDate = album.ReleaseDate
            }).ToList();
        }

        public async Task<List<TopTrackResult>> GetTopTracksByYear(int year)
        {
            var token = await _auth.GetAccessToken();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var query = Uri.EscapeDataString(year.ToString());
            var url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=10";

            var responseMessage = await _http.GetAsync(url);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {responseMessage.StatusCode}");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<SpotifySearchResponse>(json, options);

            if (response?.Tracks?.Items == null)
            {
                return new List<TopTrackResult>();
            }

            var result = response.Tracks.Items
            .Select((track, index) => new TopTrackResult
            {
                Rank = index + 1,
                Id = track.Id,
                Name = track.Name,
                Artist = track.Artists != null && track.Artists.Count > 0
                    ? string.Join(", ", track.Artists.Select(a => a.Name))
                    : "Невідомий виконавець",
                Album = track.Album?.Name ?? "Невідомий альбом"
            })
            .ToList();

            return result;
        }
        public async Task<List<RecommendationResult>> GetRecommendations(string artist, string? genre = null)
        {
            var token = await _auth.GetAccessToken();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            string query = artist;

            if (!string.IsNullOrWhiteSpace(genre))
            {
                query += " " + genre;
            }

            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=5";

            var responseMessage = await _http.GetAsync(url);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {responseMessage.StatusCode}");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<SpotifySearchResponse>(json, options);

            if (response?.Tracks?.Items == null)
            {
                return new List<RecommendationResult>();
            }

            var result = response.Tracks.Items
                .Select((track, index) => new RecommendationResult
                {
                    Rank = index + 1,
                    Id = track.Id,
                    Name = track.Name,
                    Artist = track.Artists != null && track.Artists.Count > 0
                        ? string.Join(", ", track.Artists.Select(a => a.Name))
                        : "Невідомий виконавець",
                    Album = track.Album?.Name ?? "Невідомий альбом"
                })
                .ToList();

            return result;
        }
    }
}