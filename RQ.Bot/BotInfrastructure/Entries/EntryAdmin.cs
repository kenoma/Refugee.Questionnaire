using System;
using System.Threading.Tasks;
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
        _botClient     = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }
    
    public async Task StartLaborAsync(ChatId chatId, User user)
    {
        if (chatId == null)
            return;
        
        if (!_repo.IsKnownTgUser(user.Id))
        {
            var noAuthText =
                $"Вас нет в списках доверенных пользователей. Можете попросить администраторов добавить вас, переслав им это сообщение: `id={user.Id}`";
            
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Markdown,
                text: noAuthText,
                disableWebPagePreview: false
            );
        }

        throw new NotImplementedException();
    }
}