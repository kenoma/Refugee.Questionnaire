using Bot.Repo;
using RQ.DTO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RQ.Bot.BotInfrastructure.Entry;

internal class EntryAdmin
{
    private readonly TelegramBotClient _botClient;
    private readonly IRepository _repo;
    private readonly ILogger<EntryAdmin> _logger;

    public EntryAdmin(TelegramBotClient botClient, IRepository repo, ILogger<EntryAdmin> logger)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartLaborAsync(ChatId chatId, User user)
    {
        if (chatId == null)
            return;

        if (!_repo.TryGetUserById(user.Id, out var rfUser) || !rfUser.IsAdmin)
        {
            var noAuthText =
                $"Вас нет в списках доверенных пользователей, администраторы осведомлены о вас.";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Markdown,
                text: noAuthText,
                disableWebPagePreview: false
            );

            var adminList = _repo.GetAdminUsers();

            foreach (var admin in adminList)
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Дать админа", BotResponce.Create("add_permitions", user.Id)),
                });

                await _botClient.SendTextMessageAsync(
                    chatId: admin.ChatId,
                    parseMode: ParseMode.Html,
                    text:
                    $"Пользователь {user.Username} ({user.FirstName} {user.LastName}) просит дать ему администраторские привелегии.",
                    replyMarkup: inlineKeyboard,
                    disableWebPagePreview: false
                );
            }
        }
        else
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Текущие заявки в xlsx",
                        BotResponce.Create("get_current_xlsx")),
                    InlineKeyboardButton.WithCallbackData("Все заявки в xlsx",
                        BotResponce.Create("get_all_xlsx")),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Текущие заявки в csv",
                        BotResponce.Create("get_current_csv")),
                    InlineKeyboardButton.WithCallbackData("Все заявки в csv",
                        BotResponce.Create("get_all_csv")),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Архивировать текущие заявки",
                        BotResponce.Create("archive"))
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Список администраторов",
                        BotResponce.Create("list_admins"))
                }
            });

            await _botClient.SendTextMessageAsync(
                chatId: rfUser.ChatId,
                parseMode: ParseMode.Markdown,
                text:
                $"Ваш уникальный токен для работы с API: `{rfUser.Token}`\r\n Заявки, отправленные в архив доступны к скачиванию через функцию скачивания всех заявок",
                replyMarkup: inlineKeyboard,
                disableWebPagePreview: false
            );
        }
    }

    public async Task<bool> IsAdmin(ChatId chatId, User user)
    {
        if (!_repo.GetAdminUsers().Any())
        {
            if (!_repo.TryGetUserById(user.Id, out var userData))
            {
                _repo.UpsertUser(new UserData
                {
                    ChatId = chatId.Identifier!.Value,
                    UserId = user.Id,
                    IsAdmin = true,
                    Username = user.Username!,
                    FirstName = user.FirstName,
                    LastName = user.LastName!,
                    PromotedByUser = -1,
                    Created = DateTime.Now
                });
            }
            else
            {
                userData.IsAdmin = true;
                _repo.UpsertUser(userData);
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: "Вы первый пользователь бота, вам автоматически выданы администраторские привелегии.",
                disableWebPagePreview: false
            );
        }

        return _repo.TryGetUserById(user.Id, out var rfUser) && rfUser.IsAdmin;
    }

    public async Task PromoteUserAsync(long adminUserId, long promotedUserId)
    {
        if (_repo.TryGetUserById(promotedUserId, out var rfUser) &&
            _repo.TryGetUserById(adminUserId, out var adminUser))
        {
            rfUser.IsAdmin = true;
            rfUser.PromotedByUser = adminUserId;
            _repo.UpsertUser(rfUser);

            var adminList = _repo.GetAdminUsers();

            foreach (var admin in adminList)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: admin.ChatId,
                    parseMode: ParseMode.Html,
                    text:
                    $"Пользователь @{rfUser.Username} повышен до администратора пользователем @{adminUser.Username}.",
                    disableWebPagePreview: false
                );
            }

            await _botClient.SendTextMessageAsync(
                chatId: rfUser.ChatId,
                parseMode: ParseMode.Html,
                text: $"Вам выданы права администратора пользователем @{adminUser.Username}",
                disableWebPagePreview: false
            );
        }
    }

    public async Task ArchiveAsync(ChatId chatId, User user)
    {
        if (_repo.TryGetUserById(user.Id, out var rfUser) && rfUser.IsAdmin)
        {
            _repo.ArchiveCurrentRequests();

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: "Текущие запросы отправлены в архив",
                disableWebPagePreview: false
            );
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: "Вы не являетесь администратором",
                disableWebPagePreview: false
            );
        }
    }

    public Task CreateIfNotExistUser(ChatId chatId, User user)
    {
        if (!_repo.TryGetUserById(user.Id, out var userData))
        {
            _repo.UpsertUser(new UserData
            {
                ChatId = chatId.Identifier!.Value,
                UserId = user.Id,
                IsAdmin = false,
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName!,
                PromotedByUser = -1,
                Created = DateTime.Now
            });
        }
        else
        {
            //Ветка для админа. добавленного через коммандную строку
            userData.ChatId = chatId.Identifier!.Value;
            userData.Username = user.Username;
            userData.FirstName = user.FirstName;
            userData.LastName = user.LastName;
            _repo.UpsertUser(userData);
        }

        return Task.CompletedTask;
    }

    public async Task ListAdminsApprovedByUsersAsync(ChatId messageChat, User user)
    {
        var users = _repo.GetAllUsers();
        var promotedUsers = users.Where(z => z.PromotedByUser == user.Id).Select(z =>
            InlineKeyboardButton.WithCallbackData($"{z.Username} ({z.UserId})",
                BotResponce.Create("remove_user", z.UserId)));

        var inlineKeyboard = new InlineKeyboardMarkup(promotedUsers);

        await _botClient.SendTextMessageAsync(
            chatId: messageChat,
            parseMode: ParseMode.Html,
            text:
            "Список администраторов, назначенных вами",
            replyMarkup: inlineKeyboard,
            disableWebPagePreview: false
        );
    }

    public async Task RevokeAdminAsync(ChatId messageChat, long userId)
    {
        var users = _repo.GetAllUsers();
        var revokedList = new HashSet<long>(new[] { userId });

        var counter = 0;

        while (counter != revokedList.Count)
        {
            counter = revokedList.Count;
            foreach (var user in users.Where(z => revokedList.Contains(z.PromotedByUser)))
            {
                revokedList.Add(user.UserId);
            }
        }

        var usersToRevoke = users.Where(z => revokedList.Contains(z.UserId));
        foreach (var user in usersToRevoke)
        {
            user.IsAdmin = false;
            user.PromotedByUser = 0;
            _repo.UpsertUser(user);
            await _botClient.SendTextMessageAsync(
                chatId: user.ChatId,
                parseMode: ParseMode.Html,
                text:
                "Вас лишили прав администратора",
                disableWebPagePreview: false
            );
            _logger.LogInformation("User {UserId} was removed from admin list", user.UserId);
        }
    }
}