using System.Text.Json.Serialization;

namespace ІК_51_23_Логінова_В.Р_.Models.Spotify
{
    public class SpotifySearchResponse
    {
        [JsonPropertyName("tracks")]
        public TracksContainer Tracks { get; set; }
    }

    public class TracksContainer
    {
        [JsonPropertyName("items")]
        public List<SpotifyTrack> Items { get; set; }//список треків
    }

    public class SpotifyTrack
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artists")]
        public List<SpotifyArtist> Artists { get; set; }

        [JsonPropertyName("album")]
        public SpotifyAlbum Album { get; set; }
       
    }

    public class SpotifyArtist
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class SpotifyAlbum
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

}