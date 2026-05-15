using System.Text.Json;
using ІК_51_23_Логінова_В.Р_.Models.Local;

namespace ІК_51_23_Логінова_В.Р_.Services
{
    public class FavoritesService
    {
        private readonly string _filePath;

        public FavoritesService()
        {
            _filePath = Path.Combine(AppContext.BaseDirectory, "Storage", "favorites.json");
        }

        private async Task<List<FavoriteTrack>> ReadFavoritesFromFile()//читаємо обрані треки з файлу
        {
            if (!File.Exists(_filePath))//перевіряємо чи існує файл
            {
                return new List<FavoriteTrack>();//повертає порожній список
            }

            var json = await File.ReadAllTextAsync(_filePath);

            if (string.IsNullOrWhiteSpace(json))//перевіряє чи порожній рядок
            {
                return new List<FavoriteTrack>();
            }

            var favorites = JsonSerializer.Deserialize<List<FavoriteTrack>>(json);//перетворюєио JSON-текст у список об’єктів FavoriteTrack

            return favorites ?? new List<FavoriteTrack>();//якщо зліва null, тоді берем справа
        }

        private async Task SaveFavoritesToFile(List<FavoriteTrack> favorites)//зберігаємо в обране
        {
            var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
            {
                WriteIndented = true//запис з відступами
            });

            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))//якщо шлях до папки нормальний і папки ще немає — створюємо її.
            {
                Directory.CreateDirectory(directory);//створюємо папку
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_filePath, json);//асинхронно записуєм у файл
        }

        public async Task<List<FavoriteTrack>> GetAll()//повертає всі обрані треки
        {
            return await ReadFavoritesFromFile();
        }

        public async Task<FavoriteTrack?> GetById(Guid id)
        {
            var favorites = await ReadFavoritesFromFile();
            return favorites.FirstOrDefault(f => f.Id == id);
        }

        public async Task<FavoriteTrack?> Update(Guid id, UpdateFavoriteRequest request)//оновлення рейтингу/коментаря
        {
            var favorites = await ReadFavoritesFromFile();

            var favorite = favorites.FirstOrDefault(f => f.Id == id);

            if (favorite == null)
            {
                return null;
            }

            favorite.Rating = request.Rating;
            favorite.Comment = request.Comment;

            await SaveFavoritesToFile(favorites);

            return favorite;
        }

        public async Task<bool> Delete(Guid id)
        {
            var favorites = await ReadFavoritesFromFile();

            var favorite = favorites.FirstOrDefault(f => f.Id == id);

            if (favorite == null)
            {
                return false;
            }

            favorites.Remove(favorite);
            await SaveFavoritesToFile(favorites);

            return true;
        }
        public async Task<FavoriteTrack?> AddBySpotifyId(string spotifyId, SpotifyApiService spotifyApi)
        {
            var track = await spotifyApi.GetTrackById(spotifyId);

            if (track == null)
            {
                return null;
            }

            var favorites = await ReadFavoritesFromFile();
            var existingFavorite = favorites
            .FirstOrDefault(f => f.SpotifyId == spotifyId);//перевірка чи трек вже в обраному

            if (existingFavorite != null)
            {
                return existingFavorite;
            }
            var newFavorite = new FavoriteTrack
            {
                Id = Guid.NewGuid(),
                SpotifyId = track.Id,
                Name = track.Name,
                Artist = track.Artist,
                Album = track.Album,
                Rating = 0,
                Comment = ""
            };

            favorites.Add(newFavorite);//додаємо в список
            await SaveFavoritesToFile(favorites);

            return newFavorite;
        }
    }
}