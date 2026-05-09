namespace ІК_51_23_Логінова_В.Р_.Models.Database
{
    public class UserDb
    {
        public int Id { get; set; }

        public long TelegramId { get; set; }

        public string? Username { get; set; }

        public string? FirstName { get; set; }

        public DateTime RegisteredAt { get; set; }
    }
}