using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ІК_51_23_Логінова_В.Р_.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly SpotifyApiService _spotify;
        private readonly DatabaseService _database;

        private readonly Dictionary<long, string> _userStates = new();
        private readonly Dictionary<long, string> _tempData = new();

        public TelegramBotService(SpotifyApiService spotify, DatabaseService database)
        {
            _spotify = spotify;
            _database = database;

            _botClient = new TelegramBotClient("8667986539:AAFSexpNy3LUZFBIbVUTDmuIGCS1iRU8PoM");
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions);

            Console.WriteLine("Telegram bot started");
        }

        private ReplyKeyboardMarkup GetMainMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "📝 Зареєструватись" },
                new KeyboardButton[] { "🔍 Пошук музики", "⭐ Моє обране" },
                new KeyboardButton[] { "✏️ Оцінка та нотатка" },
                new KeyboardButton[] { "🗑 Видалити трек" }
            })
            {
                ResizeKeyboard = true
            };
        }

        private InlineKeyboardMarkup GetSearchInlineMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎵 За назвою", "search_by_name"),
                    InlineKeyboardButton.WithCallbackData("👤 За виконавцем", "search_by_artist")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🆔 Деталі за ID", "search_by_id"),
                    InlineKeyboardButton.WithCallbackData("💿 Альбоми", "search_albums")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📅 За роком", "top_by_year"),
                    InlineKeyboardButton.WithCallbackData("🎧 Рекомендації", "recommendations_artist")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎼 За жанром", "recommendations_genre_artist")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔤 Сортування", "sort_query"),
                    InlineKeyboardButton.WithCallbackData("🔎 Фільтр", "filter_query")
                }
            });
        }

        private InlineKeyboardMarkup GetSortInlineMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎵 За назвою", "sort_name"),
                    InlineKeyboardButton.WithCallbackData("👤 За виконавцем", "sort_artist")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("💿 За альбомом", "sort_album")
                }
            });
        }

        private async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            if (update.CallbackQuery != null)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
                return;
            }

            if (update.Message == null || update.Message.Text == null)
            {
                return;
            }

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;
            var telegramId = update.Message.From?.Id ?? chatId;
            var username = update.Message.From?.Username;
            var firstName = update.Message.From?.FirstName;

            if (text == "/start")
            {
                await botClient.SendMessage(
                    chatId,
                    "Привіт! Я Music Search Bot 🎧\n\n" +
                    "Тут музика зберігається не просто списком — можна створювати власну колекцію улюблених треків, залишати свої враження, оцінки та швидко повертатись до пісень, які колись зачепили 🎶\n\n" +
                    "Іноді знаходиш трек, який хочеться переслуховувати знову і знову, але через деякий час він просто губиться серед сотень інших. Саме для цього і створений бот — щоб улюблена музика завжди залишалась поруч, а важливі пісні не зникали в нескінченних плейлистах.\n\n" +
                    "Тут можна збирати свою атмосферу, зберігати треки під різний настрій, залишати короткі нотатки чи асоціації, які потім приємно перечитувати. Це більше схоже на особистий музичний простір, де все зібрано в одному місці 💛",
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);

                return;
            }

            if (text == "📝 Зареєструватись")
            {
                var user = await _database.RegisterUser(telegramId, username, firstName);

                await botClient.SendMessage(
                    chatId,
                    $"Тебе зареєстровано ✅\n\nID користувача: {user.Id}",
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);

                return;
            }

            if (text == "🔍 Пошук музики")
            {
                await botClient.SendMessage(
                    chatId,
                    "Обери тип пошуку 🎧",
                    replyMarkup: GetSearchInlineMenu(),
                    cancellationToken: cancellationToken);

                return;
            }

            if (text == "⭐ Моє обране")
            {
                await ShowFavorites(botClient, chatId, telegramId, cancellationToken);
                return;
            }

            if (text == "🗑 Видалити трек")
            {
                _userStates[chatId] = "delete_favorite";

                await botClient.SendMessage(
                    chatId,
                    "Введи ID запису з обраного, який хочеш видалити:",
                    cancellationToken: cancellationToken);

                return;
            }

            if (text == "✏️ Оцінка та нотатка")
            {
                _userStates[chatId] = "update_favorite_id";

                await botClient.SendMessage(
                    chatId,
                    "Введи ID запису з обраного, який хочеш оновити:",
                    cancellationToken: cancellationToken);

                return;
            }

            await ProcessUserState(botClient, chatId, telegramId, text, cancellationToken);
        }

        private async Task HandleCallbackQuery(
            ITelegramBotClient botClient,
            CallbackQuery callback,
            CancellationToken cancellationToken)
        {
            if (callback.Message == null)
            {
                return;
            }

            var chatId = callback.Message.Chat.Id;
            var data = callback.Data;

            if (data == "search_by_name")
            {
                _userStates[chatId] = "search_by_name";
                await botClient.SendMessage(chatId, "Введи назву треку:", cancellationToken: cancellationToken);
            }
            else if (data == "search_by_artist")
            {
                _userStates[chatId] = "search_by_artist";
                await botClient.SendMessage(chatId, "Введи ім’я виконавця:", cancellationToken: cancellationToken);
            }
            else if (data == "search_by_id")
            {
                _userStates[chatId] = "search_by_id";

                await botClient.SendMessage(
                    chatId,
                    "Введи ID треку 🆔\n\nЙого можна скопіювати з результатів пошуку.",
                    cancellationToken: cancellationToken);
            }
            else if (data == "search_albums")
            {
                _userStates[chatId] = "search_albums";
                await botClient.SendMessage(chatId, "Введи назву альбому або виконавця:", cancellationToken: cancellationToken);
            }
            else if (data == "top_by_year")
            {
                _userStates[chatId] = "top_by_year";
                await botClient.SendMessage(chatId, "Введи рік, наприклад 2024:", cancellationToken: cancellationToken);
            }
            else if (data == "recommendations_artist")
            {
                _userStates[chatId] = "recommendations_artist";

                await botClient.SendMessage(
                    chatId,
                    "Введи виконавця для рекомендацій:",
                    cancellationToken: cancellationToken);
            }
            else if (data == "recommendations_genre_artist")
            {
                _userStates[chatId] = "recommendations_genre_artist";

                await botClient.SendMessage(
                    chatId,
                    "Введи виконавця:",
                    cancellationToken: cancellationToken);
            }
            else if (data == "sort_query")
            {
                _userStates[chatId] = "sort_query";

                await botClient.SendMessage(
                    chatId,
                    "Введи назву треку або виконавця, а потім обери, як відсортувати результати:",
                    cancellationToken: cancellationToken);
            }
            else if (data == "filter_query")
            {
                _userStates[chatId] = "filter_query";

                await botClient.SendMessage(
                    chatId,
                    "Введи назву треку або виконавця:",
                    cancellationToken: cancellationToken);
            }
            else if (data == "sort_name")
            {
                await ProcessSortingCallback(botClient, chatId, data, cancellationToken);
            }
            else if (data != null && data.StartsWith("add_"))
            {
                var spotifyId = data.Replace("add_", "");

                var telegramId = callback.From.Id;

                var favorite = await _database.AddFavoriteBySpotifyId(
                    telegramId,
                    spotifyId,
                    _spotify);

                if (favorite == null)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Не вдалося додати трек. Можливо, ти ще не зареєстрована(ий).",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(
                        chatId,
                        $"Трек додано в обране ✅\n\n🎵 {favorite.Name}\n👤 {favorite.Artist}",
                        cancellationToken: cancellationToken);
                }
            }
            else if (data != null && data.StartsWith("details_"))
            {
                var spotifyId = data.Replace("details_", "");

                var track = await _spotify.GetTrackById(spotifyId);

                if (track == null)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Трек не знайдено 😔",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    var minutes = track.DurationMs / 60000;
                    var seconds = (track.DurationMs % 60000) / 1000;

                    await botClient.SendMessage(
                        chatId,
                        $"🎵 {track.Name}\n" +
                        $"👤 {track.Artist}\n" +
                        $"💿 {track.Album}\n" +
                        $"⏱ Тривалість: {minutes}:{seconds:D2}",
                        cancellationToken: cancellationToken);
                }
            }

            await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);
        }

        private async Task ProcessUserState(
            ITelegramBotClient botClient,
            long chatId,
            long telegramId,
            string text,
            CancellationToken cancellationToken)
        {
            if (!_userStates.ContainsKey(chatId))
            {
                await botClient.SendMessage(
                    chatId,
                    "Я не зовсім зрозумів запит 😅 Обери дію з меню.",
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);

                return;
            }

            var state = _userStates[chatId];

            if (state == "search_by_name")
            {
                if (IsInvalidSearchText(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Некоректна назва треку.\nВведи правильну назву пісні, наприклад: Хартбіт, Without Me або Stones.",
                        cancellationToken: cancellationToken);

                    return;
                }

                var tracks = await _spotify.SearchTracks(text);

                var filteredTracks = tracks
                    .Where(t => t.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filteredTracks.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Трек із такою назвою не знайдено 😔\nСпробуй написати назву інакше.",
                        cancellationToken: cancellationToken);

                    _userStates.Remove(chatId);
                    return;
                }

                await SendTracks(botClient, chatId, filteredTracks, cancellationToken);

                _userStates.Remove(chatId);
                return;
            }

            if (state == "search_by_artist")
            {
                if (IsInvalidArtistText(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Некоректне ім’я виконавця.\nВведи назву артиста літерами, наприклад: DOROFEEVA, Eminem або The Hardkiss.",
                        cancellationToken: cancellationToken);

                    return;
                }

                var tracks = await _spotify.SearchTracks($"artist:{text}");

                if (tracks.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Такого виконавця не знайдено 😔\nСпробуй написати ім’я інакше.",
                        cancellationToken: cancellationToken);

                    _userStates.Remove(chatId);
                    return;
                }

                await SendTracks(botClient, chatId, tracks, cancellationToken);

                _userStates.Remove(chatId);
                return;
            }

            if (state == "search_by_id")
            {
                var track = await _spotify.GetTrackById(text);

                if (track == null)
                {
                    await botClient.SendMessage(chatId, "Трек не знайдено 😔", cancellationToken: cancellationToken);
                }
                else
                {
                    var minutes = track.DurationMs / 60000;
                    var seconds = (track.DurationMs % 60000) / 1000;

                    await botClient.SendMessage(
                        chatId,
                        $"🎵 {track.Name}\n" +
                        $"👤 {track.Artist}\n" +
                        $"💿 {track.Album}\n" +
                        $"⏱ Тривалість: {minutes}:{seconds:D2}",
                        cancellationToken: cancellationToken);
                }

                _userStates.Remove(chatId);
                return;
            }

            if (state == "search_albums")
            {
                if (IsInvalidSearchText(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Некоректний запит для пошуку альбомів.\nВведи назву альбому або виконавця літерами.",
                        cancellationToken: cancellationToken);

                    return;
                }

                var albums = await _spotify.SearchAlbums(text);

                var filteredAlbums = albums
                    .Where(a =>
                        a.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                        a.Artist.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filteredAlbums.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Альбоми за таким запитом не знайдено 😔\nСпробуй написати назву інакше.",
                        cancellationToken: cancellationToken);

                    _userStates.Remove(chatId);
                    return;
                }

                await SendAlbums(botClient, chatId, filteredAlbums, cancellationToken);

                _userStates.Remove(chatId);
                return;
            }

            if (state == "top_by_year")
            {
                if (!int.TryParse(text, out int year))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Рік має бути числом.",
                        cancellationToken: cancellationToken);

                    return;
                }

                if (year < 1950 || year > DateTime.Now.Year)
                {
                    await botClient.SendMessage(
                        chatId,
                        $"Введи рік у межах 1950–{DateTime.Now.Year} 🎵",
                        cancellationToken: cancellationToken);

                    return;
                }

                var tracks = await _spotify.GetTopTracksByYear(year);

                if (tracks.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "За цей рік нічого не знайдено 😔",
                        cancellationToken: cancellationToken);

                    _userStates.Remove(chatId);
                    return;
                }

                await SendTracks(botClient, chatId, tracks, cancellationToken);

                _userStates.Remove(chatId);
                return;
            }

            if (state == "recommendations_artist")
            {
                var tracks = await _spotify.GetRecommendations(text);
                await SendRecommendations(botClient, chatId, tracks, cancellationToken);

                _userStates.Remove(chatId);
                return;
            }

            if (state == "recommendations_genre_artist")
            {
                _tempData[chatId] = text;
                _userStates[chatId] = "recommendations_genre_value";

                await botClient.SendMessage(
                    chatId,
                    "Тепер введи жанр англійською, наприклад: pop, rock, rap, dance:",
                    cancellationToken: cancellationToken);

                return;
            }

            if (state == "recommendations_genre_value")
            {
                var artist = _tempData[chatId];
                var genre = text;

                var tracks = await _spotify.GetRecommendations(artist, genre);
                await SendRecommendations(botClient, chatId, tracks, cancellationToken);

                _userStates.Remove(chatId);
                _tempData.Remove(chatId);
                return;
            }

            if (state == "sort_query")
            {
                if (IsInvalidSearchText(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Некоректний пошуковий запит.\nВведи назву треку або виконавця літерами.",
                        cancellationToken: cancellationToken);

                    return;
                }

                _tempData[chatId] = text;
                _userStates[chatId] = "sort_waiting";

                await botClient.SendMessage(
                    chatId,
                    "Обери, як відсортувати результати:",
                    replyMarkup: GetSortInlineMenu(),
                    cancellationToken: cancellationToken);

                return;
            }

            if (state == "filter_query")
            {
                if (IsInvalidSearchText(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Некоректний пошуковий запит.\nВведи назву треку або виконавця.",
                        cancellationToken: cancellationToken);

                    return;
                }

                _tempData[chatId] = text;
                _userStates[chatId] = "filter_value";

                await botClient.SendMessage(
                    chatId,
                    "Введи слово, яке має бути в назві треку:",
                    cancellationToken: cancellationToken);

                return;
            }

            if (state == "filter_value")
            {
                if (IsInvalidFilterText(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Некоректне слово для фільтра.\nВведи слово літерами.",
                        cancellationToken: cancellationToken);

                    return;
                }

                var query = _tempData[chatId];
                var nameFilter = text;

                var tracks = await _spotify.SearchTracks(query, null, nameFilter);

                if (tracks.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "За таким фільтром нічого не знайдено 😔",
                        cancellationToken: cancellationToken);

                    _userStates.Remove(chatId);
                    _tempData.Remove(chatId);
                    return;
                }

                await SendTracks(botClient, chatId, tracks, cancellationToken);

                _userStates.Remove(chatId);
                _tempData.Remove(chatId);
                return;
            }

            if (state == "delete_favorite")
            {
                if (!Guid.TryParse(text, out Guid favoriteId))
                {
                    await botClient.SendMessage(chatId, "ID запису має бути у форматі Guid.", cancellationToken: cancellationToken);
                    return;
                }

                var deleted = await _database.DeleteFavorite(telegramId, favoriteId);

                if (deleted)
                {
                    await botClient.SendMessage(chatId, "Трек видалено з обраного 🗑", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(chatId, "Запис не знайдено або ти не зареєстрована.", cancellationToken: cancellationToken);
                }

                _userStates.Remove(chatId);
                return;
            }

            if (state == "update_favorite_id")
            {
                if (!Guid.TryParse(text, out Guid favoriteId))
                {
                    await botClient.SendMessage(chatId, "ID запису має бути у форматі Guid.", cancellationToken: cancellationToken);
                    return;
                }

                _tempData[chatId] = favoriteId.ToString();
                _userStates[chatId] = "update_favorite_rating";

                await botClient.SendMessage(chatId, "Введи оцінку від 0 до 10:", cancellationToken: cancellationToken);
                return;
            }

            if (state == "update_favorite_rating")
            {
                if (!int.TryParse(text, out int rating) || rating < 0 || rating > 10)
                {
                    await botClient.SendMessage(chatId, "Оцінка має бути числом від 0 до 10.", cancellationToken: cancellationToken);
                    return;
                }

                _tempData[chatId] = _tempData[chatId] + "|" + rating;
                _userStates[chatId] = "update_favorite_comment";

                await botClient.SendMessage(chatId, "Тепер введи нотатку до треку:", cancellationToken: cancellationToken);
                return;
            }

            if (state == "update_favorite_comment")
            {
                var parts = _tempData[chatId].Split('|');

                var favoriteId = Guid.Parse(parts[0]);
                var rating = int.Parse(parts[1]);
                var comment = text;

                var updated = await _database.UpdateFavorite(telegramId, favoriteId, rating, comment);

                if (updated == null)
                {
                    await botClient.SendMessage(chatId, "Не вдалося оновити запис.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(
                        chatId,
                        $"Запис оновлено ✅\n\n🎵 {updated.Name}\n⭐ Оцінка: {updated.Rating}\n📝 Нотатка: {updated.Comment}",
                        cancellationToken: cancellationToken);
                }

                _userStates.Remove(chatId);
                _tempData.Remove(chatId);
            }
        }

        private async Task ProcessSortingCallback(
            ITelegramBotClient botClient,
            long chatId,
            string data,
            CancellationToken cancellationToken)
        {
            if (!_tempData.ContainsKey(chatId))
            {
                await botClient.SendMessage(
                    chatId,
                    "Спочатку введи пошуковий запит для сортування.",
                    cancellationToken: cancellationToken);

                return;
            }

            string sortBy = data switch
            {
                "sort_name" => "name",
                "sort_artist" => "artist",
                "sort_album" => "album",
                _ => "name"
            };

            var query = _tempData[chatId];

            var tracks = await _spotify.SearchTracks(query, sortBy);

            if (tracks.Count == 0)
            {
                await botClient.SendMessage(
                    chatId,
                    "Нічого не знайдено для сортування 😔",
                    cancellationToken: cancellationToken);

                _tempData.Remove(chatId);
                _userStates.Remove(chatId);
                return;
            }

            await SendTracks(botClient, chatId, tracks, cancellationToken);

            _tempData.Remove(chatId);
            _userStates.Remove(chatId);
        }

        private async Task ShowFavorites(
            ITelegramBotClient botClient,
            long chatId,
            long telegramId,
            CancellationToken cancellationToken)
        {
            var favorites = await _database.GetFavoritesByTelegramId(telegramId);

            if (favorites == null)
            {
                await botClient.SendMessage(chatId, "Спочатку потрібно зареєструватись 📝", cancellationToken: cancellationToken);
                return;
            }

            if (favorites.Count == 0)
            {
                await botClient.SendMessage(chatId, "У тебе ще немає обраних треків ⭐", cancellationToken: cancellationToken);
                return;
            }

            var response = new StringBuilder("Твоє обране ⭐\n\n");

            int index = 1;

            foreach (var item in favorites)
            {
                response.AppendLine($"🎧 Трек #{index}");
                response.AppendLine($"🎵 {item.Name}");
                response.AppendLine($"👤 {item.Artist}");
                response.AppendLine($"💿 {item.Album}");
                response.AppendLine($"⭐ {item.Rating}/10");
                response.AppendLine($"📝 {item.Comment}");
                response.AppendLine($"🆔 {item.Id}");
                response.AppendLine("────────────");
                response.AppendLine();

                index++;
            }

            await botClient.SendMessage(chatId, response.ToString(), cancellationToken: cancellationToken);
        }

        private async Task SendTracks(
      ITelegramBotClient botClient,
      long chatId,
      IEnumerable<dynamic> tracks,
      CancellationToken cancellationToken)
        {
            var list = tracks.ToList();

            if (list.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нічого не знайдено 😔", cancellationToken: cancellationToken);
                return;
            }

            int index = 1;

            foreach (var track in list.Take(5))
            {
                var text =
                    $"🎧 Трек #{index}\n" +
                    $"🎵 {track.Name}\n" +
                    $"👤 {track.Artist}\n" +
                    $"💿 {track.Album}";

                var buttons = new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⭐ Додати в обране", $"add_{track.Id}"),
                InlineKeyboardButton.WithCallbackData("🆔 Деталі", $"details_{track.Id}")
            }
        });

                await botClient.SendMessage(
                    chatId,
                    text,
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken);

                index++;
            }
        }

        private async Task SendAlbums(
            ITelegramBotClient botClient,
            long chatId,
            IEnumerable<dynamic> albums,
            CancellationToken cancellationToken)
        {
            var list = albums.ToList();

            var response = new StringBuilder("Знайдені альбоми 💿\n\n");

            int index = 1;

            foreach (var album in list.Take(5))
            {
                response.AppendLine($"💿 Альбом #{index}");
                response.AppendLine($"🎵 {album.Name}");
                response.AppendLine($"👤 {album.Artist}");
                response.AppendLine($"📅 {album.ReleaseDate}");
                response.AppendLine("────────────");
                response.AppendLine();

                index++;
            }

            await botClient.SendMessage(chatId, response.ToString(), cancellationToken: cancellationToken);
        }

        private async Task SendRecommendations(
            ITelegramBotClient botClient,
            long chatId,
            IEnumerable<dynamic> tracks,
            CancellationToken cancellationToken)
        {
            var list = tracks.ToList();

            if (list.Count == 0)
            {
                await botClient.SendMessage(chatId, "Рекомендації не знайдено 😔", cancellationToken: cancellationToken);
                return;
            }

            var response = new StringBuilder("Рекомендації 🎧\n\n");

            int index = 1;

            foreach (var track in list.Take(5))
            {
                response.AppendLine($"🎧 Трек #{index}");
                response.AppendLine($"🎵 {track.Name}");
                response.AppendLine($"👤 {track.Artist}");
                response.AppendLine($"💿 {track.Album}");
                response.AppendLine("────────────");
                response.AppendLine();

                index++;
            }

            await botClient.SendMessage(chatId, response.ToString(), cancellationToken: cancellationToken);
        }

        private bool IsInvalidSearchText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ||
                   text.Length < 2 ||
                   text.Any(char.IsDigit) ||
                   text.Any(char.IsSymbol) ||
                   text.Any(char.IsPunctuation) ||
                   text.Count(char.IsLetter) < 3;
        }

        private bool IsInvalidArtistText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ||
                   text.Length < 2 ||
                   text.Any(char.IsDigit) ||
                   text.Any(char.IsSymbol) ||
                   text.Any(char.IsPunctuation);
        }

        private bool IsInvalidFilterText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ||
                   text.Length < 2 ||
                   text.Any(char.IsDigit) ||
                   text.Any(char.IsSymbol) ||
                   text.Any(char.IsPunctuation) ||
                   text.Count(char.IsLetter) < 2;
        }

        private Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message);
            return Task.CompletedTask;
        }
    }
}