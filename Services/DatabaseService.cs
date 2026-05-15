using Npgsql;
using ІК_51_23_Логінова_В.Р_.Models.Database;

namespace ІК_51_23_Логінова_В.Р_.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=17041704;Database=music_search_bot_db";//підключення до бд

        public async Task<UserDb?> GetUserByTelegramId(long telegramId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT id, telegram_id, username, first_name, registered_at FROM users WHERE telegram_id = @telegram_id";
            //SQL-запит до бд
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@telegram_id", telegramId);

            await using var reader = await command.ExecuteReaderAsync();//виконання SQL-запиту, який повертає дані

            if (await reader.ReadAsync())
            {
                return new UserDb
                {
                    Id = reader.GetInt32(0),
                    TelegramId = reader.GetInt64(1),
                    Username = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    RegisteredAt = reader.GetDateTime(4)
                };
            }

            return null;
        }

        public async Task<UserDb> RegisterUser(long telegramId, string? username, string? firstName)
        {
            var existingUser = await GetUserByTelegramId(telegramId);

            if (existingUser != null)
            {
                return existingUser;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
        INSERT INTO users (telegram_id, username, first_name)
        VALUES (@telegram_id, @username, @first_name)
        RETURNING id, telegram_id, username, first_name, registered_at";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@telegram_id", telegramId);
            command.Parameters.AddWithValue("@username", username ?? "");
            command.Parameters.AddWithValue("@first_name", firstName ?? "");

            await using var reader = await command.ExecuteReaderAsync();

            await reader.ReadAsync();

            return new UserDb
            {
                Id = reader.GetInt32(0),
                TelegramId = reader.GetInt64(1),
                Username = reader.IsDBNull(2) ? null : reader.GetString(2),
                FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                RegisteredAt = reader.GetDateTime(4)
            };
        }

        public async Task<List<FavoriteDb>?> GetFavoritesByTelegramId(long telegramId)
        {
            var user = await GetUserByTelegramId(telegramId);

            if (user == null)
            {
                return null;
            }

            var favorites = new List<FavoriteDb>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
            SELECT id, user_id, spotify_id, name, artist, album, rating, comment
            FROM favorites
            WHERE user_id = @user_id";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@user_id", user.Id);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                favorites.Add(new FavoriteDb
                {
                    Id = reader.GetGuid(0),
                    UserId = reader.GetInt32(1),
                    SpotifyId = reader.GetString(2),
                    Name = reader.GetString(3),
                    Artist = reader.GetString(4),
                    Album = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Rating = reader.GetInt32(6),
                    Comment = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }

            return favorites;
        }

        public async Task<FavoriteDb?> AddFavoriteBySpotifyId(
        long telegramId,
        string spotifyId,
        SpotifyApiService spotifyApi)
        {
            var user = await GetUserByTelegramId(telegramId);//шукає користувача

            if (user == null)
            {
                return null;
            }

            var track = await spotifyApi.GetTrackById(spotifyId);

            if (track == null)
            {
                return null;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkSql = @"
            SELECT id, user_id, spotify_id, name, artist, album, rating, comment
            FROM favorites
            WHERE user_id = @user_id AND spotify_id = @spotify_id";

            await using (var checkCommand = new NpgsqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@user_id", user.Id);
                checkCommand.Parameters.AddWithValue("@spotify_id", spotifyId);

                await using var reader = await checkCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new FavoriteDb
                    {
                        Id = reader.GetGuid(0),
                        UserId = reader.GetInt32(1),
                        SpotifyId = reader.GetString(2),
                        Name = reader.GetString(3),
                        Artist = reader.GetString(4),
                        Album = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Rating = reader.GetInt32(6),
                        Comment = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    };
                }
            }

            var newId = Guid.NewGuid();

            var insertSql = @"
            INSERT INTO favorites (id, user_id, spotify_id, name, artist, album, rating, comment)
            VALUES (@id, @user_id, @spotify_id, @name, @artist, @album, @rating, @comment)
            RETURNING id, user_id, spotify_id, name, artist, album, rating, comment";

            await using var insertCommand = new NpgsqlCommand(insertSql, connection);

            insertCommand.Parameters.AddWithValue("@id", newId);
            insertCommand.Parameters.AddWithValue("@user_id", user.Id);
            insertCommand.Parameters.AddWithValue("@spotify_id", track.Id);
            insertCommand.Parameters.AddWithValue("@name", track.Name);
            insertCommand.Parameters.AddWithValue("@artist", track.Artist);
            insertCommand.Parameters.AddWithValue("@album", track.Album);
            insertCommand.Parameters.AddWithValue("@rating", 0);
            insertCommand.Parameters.AddWithValue("@comment", "");

            await using var insertReader = await insertCommand.ExecuteReaderAsync();

            await insertReader.ReadAsync();

            return new FavoriteDb
            {
                Id = insertReader.GetGuid(0),
                UserId = insertReader.GetInt32(1),
                SpotifyId = insertReader.GetString(2),
                Name = insertReader.GetString(3),
                Artist = insertReader.GetString(4),
                Album = insertReader.IsDBNull(5) ? "" : insertReader.GetString(5),
                Rating = insertReader.GetInt32(6),
                Comment = insertReader.IsDBNull(7) ? "" : insertReader.GetString(7)
            };
        }
        public async Task<FavoriteDb?> UpdateFavorite(
        long telegramId,
        Guid favoriteId,
        int rating,
        string? comment)
        {
            var user = await GetUserByTelegramId(telegramId);

            if (user == null)
            {
                return null;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
            UPDATE favorites
            SET rating = @rating,
            comment = @comment
            WHERE id = @id AND user_id = @user_id
            RETURNING id, user_id, spotify_id, name, artist, album, rating, comment";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", favoriteId);
            command.Parameters.AddWithValue("@user_id", user.Id);
            command.Parameters.AddWithValue("@rating", rating);
            command.Parameters.AddWithValue("@comment", comment ?? "");

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new FavoriteDb
                {
                    Id = reader.GetGuid(0),
                    UserId = reader.GetInt32(1),
                    SpotifyId = reader.GetString(2),
                    Name = reader.GetString(3),
                    Artist = reader.GetString(4),
                    Album = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Rating = reader.GetInt32(6),
                    Comment = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
            }

            return null;
        }

        public async Task<bool> DeleteFavorite(long telegramId, Guid favoriteId)
        {
            var user = await GetUserByTelegramId(telegramId);

            if (user == null)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
            DELETE FROM favorites
            WHERE id = @id AND user_id = @user_id";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", favoriteId);
            command.Parameters.AddWithValue("@user_id", user.Id);

            var deletedRows = await command.ExecuteNonQueryAsync();

            return deletedRows > 0;
        }

        public async Task<FavoriteDb?> GetFavoriteById(long telegramId, Guid favoriteId)
        {
            var user = await GetUserByTelegramId(telegramId);

            if (user == null)
            {
                return null;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
            SELECT id, user_id, spotify_id, name, artist, album, rating, comment
            FROM favorites
            WHERE id = @id AND user_id = @user_id";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", favoriteId);
            command.Parameters.AddWithValue("@user_id", user.Id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new FavoriteDb
                {
                    Id = reader.GetGuid(0),
                    UserId = reader.GetInt32(1),
                    SpotifyId = reader.GetString(2),
                    Name = reader.GetString(3),
                    Artist = reader.GetString(4),
                    Album = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Rating = reader.GetInt32(6),
                    Comment = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
            }

            return null;
        }
    }
}