namespace ІК_51_23_Логінова_В.Р_.Models.Spotify
{
    public class SpotifyAlbumSearchResponse//відповідь Spotify на пошук альбомів
    {
        public Albums Albums { get; set; }
    }

    public class Albums
    {
        public List<AlbumItem> Items { get; set; }//список альбомів
    }

    public class AlbumItem//один альбом
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