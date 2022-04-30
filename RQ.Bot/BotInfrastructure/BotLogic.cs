using Prometheus;
using RQ.Bot.BotInfrastructure.Entry;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RQ.Bot.BotInfrastructure
{
    /// <inheritdoc />
    internal class BotLogic : IUpdateHandler
    {
        private readonly TelegramBotClient _bot;
        private readonly EntryAdmin _entryAdmin;
        private readonly EntryQuestionnaire _entryQuestionnaire;
        private readonly ILogger<BotLogic> _logger;

        private static readonly Counter CommandsCount =
            Metrics.CreateCounter("commands_total", "Количество команд, отработанных ботом");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="entryAdmin"></param>
        /// <param name="entryQuestionnaire"></param>
        /// <param name="logger"></param>
        public BotLogic(
            TelegramBotClient botClient,
            EntryAdmin entryAdmin,
            EntryQuestionnaire entryQuestionnaire,
            ILogger<BotLogic> logger
        )
        {
            _bot = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _entryAdmin = entryAdmin ?? throw new ArgumentNullException(nameof(entryAdmin));
            _entryQuestionnaire = entryQuestionnaire ?? throw new ArgumentNullException(nameof(entryQuestionnaire));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            try
            {
                var handler = update.Type switch
                {
                    UpdateType.Message => BotOnMessageReceived(update.Message!),
                    UpdateType.EditedMessage => BotOnMessageReceived(update.Message!),
                    UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
                    UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery!),
                    UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
                    UpdateType.PollAnswer => BotOnPollAnswer(update.PollAnswer!),
                    // UpdateType.Unknown:
                    // UpdateType.ChannelPost:
                    // UpdateType.EditedChannelPost:
                    // UpdateType.ShippingQuery:
                    // UpdateType.PreCheckoutQuery:
                    // UpdateType.Poll:
                    _ => UnknownUpdateHandlerAsync(update)
                };

                await handler;
                CommandsCount.Inc();
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private Task BotOnPollAnswer(PollAnswer updatePollAnswer)
        {
            _logger.LogInformation("New vote {@Vote}", updatePollAnswer);
            return Task.CompletedTask;
        }

        private async Task BotOnMessageReceived(Message? message)
        {
            if (message == null)
                return;

            var user = message.From;
            if (user == null)
                return;

            var chatMember = await _bot.GetChatMemberAsync(message.Chat.Id, user.Id);

            _logger.LogInformation("Receive message type: {MessageType}: {MessageText} from {Member}", message.Type,
                message.Text, chatMember.User.Username);

            if (message.Type != MessageType.Text)
                return;

            if (string.IsNullOrEmpty(message.Text))
                return;

            if (message.Entities?.All(z => z.Type != MessageEntityType.BotCommand) ?? true)
                return;

            for (var entity = 0; entity < message.Entities.Length; entity++)
            {
                if (message.Entities[entity].Type != MessageEntityType.BotCommand)
                    continue;

                _logger.LogInformation("Recognized bot command: {Command}", message.EntityValues!.ElementAt(entity));

                var action = (message.EntityValues!.ElementAt(entity)
                        .Split(new[] { " ", "@" }, StringSplitOptions.RemoveEmptyEntries).First()) switch
                    {
                        "/admin" => _entryAdmin.StartLaborAsync(message.Chat!, user),
                        "/request" => _entryQuestionnaire.StartQuestionnaire(user),
                        _ => Usage(message)
                    };

                await action;

                async Task Usage(Message? msg)
                {
                    if (msg == null)
                        return;

                    const string usage = "*Доступные команды:*\r\n" +
                                         "/admin - Доступ к административным функциям\r\n" +
                                         "/request - Заполнение новой анкеты\r\n";

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Административные функции", BotResponce.Create("admin")),
                        InlineKeyboardButton.WithCallbackData("Новая анкета", BotResponce.Create("request")),
                    });

                    await _bot.SendTextMessageAsync(
                        chatId: msg.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: usage,
                        replyMarkup: inlineKeyboard,
                        disableWebPagePreview: false
                    );
                }
            }
        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            try
            {
                var user = callbackQuery.From;

                var responce = BotResponce.FromString(callbackQuery.Data!);

                _logger.LogInformation("Received callback {@Callback}", responce);

                switch (responce.Entry)
                {
                    case "admin":
                        await _entryAdmin.StartLaborAsync(callbackQuery.Message?.Chat!, user);

                        break;

                    case "request":
                        await _entryQuestionnaire.StartQuestionnaire(user);

                        break;
                }
            }
            catch (Exception e)
            {
                await HandleErrorAsync(_bot, e, CancellationToken.None);
            }
        }

        private Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
        {
            _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);
            return Task.CompletedTask;
        }

        private Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
        {
            _logger.LogInformation("Received inline result: {ChosenInlineResultResultId}", chosenInlineResult.ResultId);
            return Task.CompletedTask;
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            _logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(exception, "Bot error {ErrorMessage}", errorMessage);

            return Task.CompletedTask;
        }
    }
}