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

        private async Task<List<FavoriteTrack>> ReadFavoritesFromFile()
        {
            if (!File.Exists(_filePath))
            {
                return new List<FavoriteTrack>();
            }

            var json = await File.ReadAllTextAsync(_filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<FavoriteTrack>();
            }

            var favorites = JsonSerializer.Deserialize<List<FavoriteTrack>>(json);

            return favorites ?? new List<FavoriteTrack>();
        }

        private async Task SaveFavoritesToFile(List<FavoriteTrack> favorites)
        {
            var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_filePath, json);
        }

        public async Task<List<FavoriteTrack>> GetAll()
        {
            return await ReadFavoritesFromFile();
        }

        public async Task<FavoriteTrack?> GetById(Guid id)
        {
            var favorites = await ReadFavoritesFromFile();
            return favorites.FirstOrDefault(f => f.Id == id);
        }

        public async Task<FavoriteTrack?> Update(Guid id, UpdateFavoriteRequest request)
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

            favorites.Add(newFavorite);
            await SaveFavoritesToFile(favorites);

            return newFavorite;
        }
    }
}