namespace ІК_51_23_Логінова_В.Р_.Models.Database
{
    public class FavoriteDb
    {
        public Guid Id { get; set; }

        public int UserId { get; set; }

        public string SpotifyId { get; set; }

        public string Name { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }
    }
}