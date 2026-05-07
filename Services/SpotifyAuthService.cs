using Microsoft.Extensions.Options;
using ІК_51_23_Логінова_В.Р_.Models.Config;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ІК_51_23_Логінова_В.Р_.Services
{
    public class SpotifyAuthService//авторизація в Spotify
    {
        private readonly SpotifySettings _settings;//зберігаємо id та ключ
        private readonly HttpClient _http;

        public SpotifyAuthService(IOptions<SpotifySettings> settings)
        {
            _settings = settings.Value;
            _http = new HttpClient();
        }

        public async Task<string> GetAccessToken()//повертає токен
        {
            var auth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}")
            );

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            var data = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"}//токен для сервера
            };

            var response = await _http.PostAsync(
              "https://accounts.spotify.com/api/token",
               new FormUrlEncodedContent(data) );

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Не вдалося отримати токен Spotify. Код помилки: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);//можливість звертатись до окремих полів
            return doc.RootElement.GetProperty("access_token").GetString();
        }
    }
}