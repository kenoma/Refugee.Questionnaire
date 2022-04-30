using System;
using System.Threading.Tasks;
using Bot.Repo;
using RQ.DTO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RQ.Bot.BotInfrastructure.Entry;

internal class EntryConfigureChat
{
    private readonly TelegramBotClient _botClient;
    private readonly IRepository _states;

    public EntryConfigureChat(TelegramBotClient botClient, IRepository states)
    {
        _botClient     = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _states = states ?? throw new ArgumentNullException(nameof(states));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task SendConfiguration(Message message)
    {
        await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
        _states.TryGetChatData(message.Chat.Id, out var cfg);

        var splits = message.Text.Split(' ');

        if (splits.Length == 3)
        {
            if (cfg == null)
            {
                cfg = new ChatConfiguration { ChatId = message.Chat.Id };
                //_states.CreateNewConfig(message.Chat.Id);
            }

            if (splits[1] == "host")
            {
                //_states.SetGitlabHost(message.Chat.Id, splits[2]);
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Хорошо, гитлаб теперь по адресу: {splits[2]}"
                );
            }

            if (splits[1] == "token")
            {
                //_states.SetGitlabSecret(message.Chat.Id, splits[2]);

                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Секрет сохраню!",
                    replyToMessageId: message.MessageId
                );
            }

            if (splits[1] == "hook_token")
            {
                //_states.SetWebHookToken(message.Chat.Id, splits[2]);

                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Буду публковать все веб-хуки с токеном {splits[2]}!",
                    replyToMessageId: message.MessageId
                );
            }
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                parseMode:ParseMode.Markdown,
                text: "Понимаю только `[host, token, hook_token]`\r\n" +
                      "`[host, token]` - основные параметры для бота, "+
                      "`[hook_token]` - параметры для работы с веб-хуками"
            );
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            parseMode: ParseMode.Markdown,
            text: cfg?.ToString() ?? "Нужно сконфигурировать меня"
        );
    }
}