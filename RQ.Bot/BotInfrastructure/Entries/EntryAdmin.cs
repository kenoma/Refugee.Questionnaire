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

    public EntryAdmin(TelegramBotClient botClient, IRepository repo)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public async Task StartLaborAsync(ChatId chatId, User user)
    {
        if (chatId == null)
            return;

        if (!_repo.TryGetUserById(user.Id, out var rfUser) || !rfUser.IsAdmin)
        {
            _repo.UpsertUser(new UserData
            {
                ChatId = chatId.Identifier!.Value,
                UserId = user.Id,
                IsAdmin = false,
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName!
            });

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
            _repo.UpsertUser(new UserData
            {
                ChatId = chatId.Identifier!.Value,
                UserId = user.Id,
                IsAdmin = true,
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName!,
                PromotedByUser = -1
            });

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
        if (_repo.TryGetUserById(promotedUserId, out var rfUser))
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
                    text: $"Пользователь @{rfUser.Username} повышен до администратора.",
                    disableWebPagePreview: false
                );
            }

            await _botClient.SendTextMessageAsync(
                chatId: rfUser.ChatId,
                parseMode: ParseMode.Html,
                text: $"Вам выданы права администратор",
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
        _repo.UpsertUser(new UserData
        {
            ChatId = chatId.Identifier!.Value,
            UserId = user.Id,
            IsAdmin = false,
            Username = user.Username!,
            FirstName = user.FirstName,
            LastName = user.LastName!,
            PromotedByUser = -1
        });
        return Task.CompletedTask;
    }
}