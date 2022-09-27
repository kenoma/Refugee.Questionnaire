using Bot.Repo;
using RQ.Bot.BotInfrastructure.Entry;
using RQ.DTO;
using RQ.DTO.Enum;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RQ.Bot.BotInfrastructure.Entries;

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

        if (!_repo.TryGetUserById(user.Id, out var rfUser) || !rfUser.IsAdministrator)
        {
            var noAuthText =
                $"Вас нет в списках доверенных пользователей, администраторы осведомлены о вас.";

            await SendMessageToUser(chatId, noAuthText);
        }
        else
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Текущие заявки в xlsx",
                        BotResponce.Create(BotResponceType.CurrentXlsx)),
                    InlineKeyboardButton.WithCallbackData("Все заявки в xlsx",
                        BotResponce.Create(BotResponceType.AllXlsx)),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Текущие заявки в csv",
                        BotResponce.Create(BotResponceType.CurrentCsv)),
                    InlineKeyboardButton.WithCallbackData("Все заявки в csv",
                        BotResponce.Create(BotResponceType.AllCsv)),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Архивировать текущие заявки",
                        BotResponce.Create(BotResponceType.Archive))
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Список администраторов",
                        BotResponce.Create(BotResponceType.ListAdmins))
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"Уведомления о новых анкетах:{(rfUser.IsNotificationsOn ? "ВКЛ" : "ВЫКЛ")}",
                        BotResponce.Create(BotResponceType.SwitchNotifications, !rfUser.IsNotificationsOn))
                }
            });

            await SendMessageToUser(rfUser.ChatId,
                $"Ваш уникальный токен для работы с API: `{rfUser.Token}`\r\n Заявки, отправленные в архив доступны к скачиванию через функцию скачивания всех заявок",
                inlineKeyboard);
        }
    }

    public async Task<bool> IsAdmin(ChatId chatId, User user)
    {
        if (!_repo.TryGetUserById(user.Id, out _))
        {
            _repo.UpsertUser(new UserData
            {
                ChatId = chatId.Identifier!.Value,
                UserId = user.Id,
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName!,
                Created = DateTime.Now
            });
        }

        return _repo.TryGetUserById(user.Id, out var rfUser) && rfUser.IsAdministrator;
    }
    
    public async Task ArchiveAsync(ChatId chatId, User user)
    {
        if (_repo.TryGetUserById(user.Id, out var rfUser) && rfUser.IsAdministrator)
        {
            _repo.ArchiveCurrentRequests();

            await SendMessageToUser(chatId, "Текущие запросы отправлены в архив");
        }
        else
        {
            await SendMessageToUser(chatId, "Вы не являетесь администратором");
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
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName!,
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

    private async Task SendMessageToUser(ChatId chatId, string msg, InlineKeyboardMarkup inlineKeyboardMarkup = default)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: msg,
                disableWebPagePreview: false,
                replyMarkup: inlineKeyboardMarkup
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to send message to {ChatId} : {Reason}", chatId, e.Message);
        }
    }

    public async Task WaitForMessageToAdminsAsync(Chat chatId, User user)
    {
        if (_repo.TryGetUserById(user.Id, out var userData))
        {
            userData.IsMessageToAdminsRequest = true;

            _repo.UpsertUser(userData);
            await SendMessageToUser(chatId, "Напишите сообщение, которое будет передано администраторам:");
            _logger.LogInformation("User {UserId} asks for help", user.Id);
        }
        else
        {
            _logger.LogInformation("Failed to get user {UserId} data", user.Id);
        }
    }

    public async Task WaitForMessageToUsersAsync(Chat chatId, User user, long userToReply)
    {
        if (_repo.TryGetUserById(user.Id, out var userData))
        {
            userData.UserToReply = userToReply;

            _repo.UpsertUser(userData);
            await SendMessageToUser(chatId, "Что ответить пользователю?");
            _logger.LogInformation("Admin {UserId} going to reply to user", user.Id);
        }
        else
        {
            _logger.LogInformation("Failed to get user {UserId} data", user.Id);
        }
    }

    public async Task<bool> IsMessageRequest(long chatId, long userId, string messageText)
    {
        if (!_repo.TryGetUserById(userId, out var userData))
            return false;

        if (!userData.IsMessageToAdminsRequest)
            return false;

        userData.IsMessageToAdminsRequest = false;
        _repo.UpsertUser(userData);

        var adminList = _repo.GetAdminUsers();

        var promotedUsers = InlineKeyboardButton.WithCallbackData($"Ответить пользователю",
            BotResponce.Create(BotResponceType.ReplyToUser, userData.UserId));

        var inlineKeyboard = new InlineKeyboardMarkup(promotedUsers);

        foreach (var admin in adminList)
        {
            await SendMessageToUser(admin.ChatId,
                $"Пользователь @{userData.Username} ({userData.UserId}) отправил сообщение администраторам: {messageText}",
                inlineKeyboard);
        }

        await SendMessageToUser(chatId, "Ваше сообщение отправлено администраторам.");
        _logger.LogInformation("User {UserId} send message: {MessageText}", userId, messageText);
        return true;
    }

    public async Task<bool> IsUserReplied(long userId, string messageText)
    {
        if (!_repo.TryGetUserById(userId, out var userData))
            return false;

        if (userData.UserToReply == 0)
            return false;

        var targetUser = userData.UserToReply;
        userData.UserToReply = 0;
        _repo.UpsertUser(userData);

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Ответить", BotResponce.Create(BotResponceType.MessageToAdmins)),
        });

        await SendMessageToUser(targetUser, messageText, inlineKeyboard);

        var adminList = _repo.GetAdminUsers();

        foreach (var admin in adminList)
        {
            await SendMessageToUser(admin.ChatId,
                $"Администратор @{userData.Username} ({userData.UserId}) отправил сообщение пользователю ({targetUser}): {messageText}");
        }

        _logger.LogInformation("User {UserId} send message: {MessageText}", userId, messageText);
        return true;
    }

    public async Task SwitchNotificationsToUserAsync(Chat messageChat, User user, bool isNotificationsOn)
    {
        if (_repo.TryGetUserById(user.Id, out var userData) && userData.IsAdministrator)
        {
            userData.IsNotificationsOn = isNotificationsOn;

            _repo.UpsertUser(userData);
            await SendMessageToUser(messageChat.Id,
                isNotificationsOn
                    ? "Вы теперь будете получать уведомления о новых анкетах"
                    : "Вы отключили уведомления");
            _logger.LogInformation("User {UserId} switches notifications to {IsNotAll}", user.Id, isNotificationsOn);
        }
    }
}