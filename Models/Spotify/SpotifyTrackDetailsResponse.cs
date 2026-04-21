using System.Text.Json.Serialization;

namespace ІК_51_23_Логінова_В.Р_.Models.Spotify
{
    public class SpotifyTrackDetailsResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artists")]
        public List<SpotifyArtist> Artists { get; set; }

        [JsonPropertyName("album")]
        public SpotifyAlbum Album { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }
       
    }
}