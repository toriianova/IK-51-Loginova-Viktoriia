namespace ІК_51_23_Логінова_В.Р_.Models.Local
{
    public class FavoriteTrack//улюблений трек
    {
        public Guid Id { get; set; }//для кожного треку унікальне id
        public string SpotifyId { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public int Rating { get; set; }//оцінка
        public string Comment { get; set; }//нотатка
    }
}