namespace ІК_51_23_Логінова_В.Р_.Models.Spotify
{
    public class SpotifyAlbumSearchResponse
    {
        public Albums Albums { get; set; }
    }

    public class Albums
    {
        public List<AlbumItem> Items { get; set; }
    }

    public class AlbumItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ReleaseDate { get; set; }
        public List<Artist> Artists { get; set; }
    }

    public class Artist
    {
        public string Name { get; set; }
    }
}