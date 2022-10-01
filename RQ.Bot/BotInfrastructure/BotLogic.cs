using System.Text;
using Prometheus;
using RQ.Bot.BotInfrastructure.Entries;
using RQ.Bot.BotInfrastructure.Entry;
using RQ.DTO.Enum;
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
        private readonly EntryDownloadCsv _entryDownloadCsv;
        private readonly ILogger<BotLogic> _logger;

        private static readonly Counter CommandsCount =
            Metrics.CreateCounter("commands_total", "Количество команд, отработанных ботом");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="entryAdmin"></param>
        /// <param name="entryQuestionnaire"></param>
        /// <param name="entryDownloadCsv"></param>
        /// <param name="logger"></param>
        public BotLogic(
            TelegramBotClient botClient,
            EntryAdmin entryAdmin,
            EntryQuestionnaire entryQuestionnaire,
            EntryDownloadCsv entryDownloadCsv,
            ILogger<BotLogic> logger
        )
        {
            _bot = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _entryAdmin = entryAdmin ?? throw new ArgumentNullException(nameof(entryAdmin));
            _entryQuestionnaire = entryQuestionnaire ?? throw new ArgumentNullException(nameof(entryQuestionnaire));
            _entryDownloadCsv = entryDownloadCsv ?? throw new ArgumentNullException(nameof(entryDownloadCsv));
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
                    // UpdateType.Poll => BotOnPoll(update.Poll!),
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


        private async Task BotOnPollAnswer(PollAnswer updatePollAnswer)
        {
            _logger.LogInformation("New vote {@Vote}", updatePollAnswer);
            await _entryQuestionnaire.ProcessPoll(updatePollAnswer);
        }

        private async Task BotOnMessageReceived(Message message)
        {
            var user = message?.From;
            if (user == null)
                return;

            var chatMember = await _bot.GetChatMemberAsync(message.Chat.Id, user.Id);
            await _entryAdmin.CreateIfNotExistUser(message.Chat.Id, user);

            _logger.LogInformation("Receive message type: {MessageType}: {MessageText} from {Member} ({UserId})",
                message.Type,
                message.Text, chatMember.User.Username, chatMember.User.Id);

            if (message.Type != MessageType.Text)
                return;

            if (await _entryAdmin.IsMessageRequest(message.Chat.Id, user.Id, message.Text!))
            {
                return;
            }

            if (await _entryAdmin.IsUserReplied(user.Id, message.Text!))
            {
                return;
            }

            if ((message.Entities?.All(z => z.Type != MessageEntityType.BotCommand) ?? true) &&
                (await _entryQuestionnaire.TryProcessStateMachineAsync(message.Chat.Id, user.Id, message.Text!)))
            {
                return;
            }

            if (message.Entities?.Any(z => z.Type == MessageEntityType.BotCommand) ?? true)
                await _entryQuestionnaire.InterruptCurrentQuest(message.Chat.Id, user.Id);

            if (message.Entities?.All(z => z.Type != MessageEntityType.BotCommand) ?? true)
            {
                await Usage(message);
                return;
            }

            for (var entity = 0; entity < message.Entities.Length; entity++)
            {
                if (message.Entities[entity].Type != MessageEntityType.BotCommand)
                    continue;

                _logger.LogInformation("Recognized bot command: {Command}", message.EntityValues!.ElementAt(entity));

                var action = (message.EntityValues!.ElementAt(entity)
                        .Split(new[] { " ", "@" }, StringSplitOptions.RemoveEmptyEntries).First()) switch
                    {
                        "/admin" => _entryAdmin.StartLaborAsync(message.Chat!, user),
                        "/request" => _entryQuestionnaire.FillLatestRequestAsync(message.Chat!, user),
                        _ => Usage(message)
                    };

                await action;
            }
        }

        private async Task Usage(Message msg)
        {
            if (msg == null)
                return;

            var isUserAdmin = await _entryAdmin.IsAdmin(msg.Chat!, msg.From!);
            var requests = _entryQuestionnaire.GetAllUserRequest(msg.From!);

            var usage = new StringBuilder(
                    $"Ваш уровень доступа: {(isUserAdmin ? "*администраторский*" : "пользовательский")}\r\n")
                .AppendLine($"Вы заполнили {requests.Length} заявок\r\n")
                .AppendLine($"Последние 10:\r\n")
                .AppendJoin("\r\n",
                    requests.Take(10).Select(z =>
                        $"Дата `{z.TimeStamp:dd.MM.yyyy hh:mm}` Статус `{((z.IsCompleted && !z.IsInterrupted) ? "заполнена" : "некорректная (прерванная)")}`"))
                .AppendLine("\r\n`------------------------------------------`\r\n")
                .ToString();

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Новая анкета", BotResponce.Create(BotResponceType.FillRequest)),
                InlineKeyboardButton.WithCallbackData("Написать администраторам",
                    BotResponce.Create(BotResponceType.MessageToAdmins)),
            });

            await _bot.SendTextMessageAsync(
                chatId: msg.Chat.Id,
                parseMode: ParseMode.Markdown,
                text: usage,
                replyMarkup: inlineKeyboard,
                disableWebPagePreview: false
            );
        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            try
            {
                var messageChat = callbackQuery.Message?.Chat;
                
                if(messageChat is null)
                    return;
                
                var user = callbackQuery.From;

                var responce = BotResponce.FromString(callbackQuery.Data!);

                _logger.LogInformation("Received callback {Callback}: {Payload}", responce.E, responce.P);

                
                
                switch (responce.E)
                {
                    case BotResponceType.FillRequest:
                        await _entryQuestionnaire.FillLatestRequestAsync(messageChat, user);
                        break;

                    case BotResponceType.CurrentCsv:
                        await _entryDownloadCsv.GetRequestsInCsvAsync(messageChat, false, user);
                        break;

                    case BotResponceType.AllCsv:
                        await _entryDownloadCsv.GetRequestsInCsvAsync(messageChat, true, user);
                        break;

                    case BotResponceType.CurrentXlsx:
                        await _entryDownloadCsv.GetRequestsInXlsxAsync(messageChat, false, user);
                        break;

                    case BotResponceType.AllXlsx:
                        await _entryDownloadCsv.GetRequestsInXlsxAsync(messageChat, true, user);
                        break;

                    case BotResponceType.Archive:
                        await _entryAdmin.ArchiveAsync(messageChat, user);
                        break;

                    case BotResponceType.QFinish:
                        await _entryQuestionnaire.CompleteAsync(messageChat, user.Id);
                        break;

                    case BotResponceType.QReturn:
                        await _entryQuestionnaire.ReturnToRootAsync(messageChat, user.Id);
                        break;

                    case BotResponceType.QMove:
                        await _entryQuestionnaire.MoveMenuAsync(messageChat, user, responce.P);
                        break;

                    case BotResponceType.QRem:
                        await _entryQuestionnaire.RemoveAnswersForCategoryAsync(messageChat, user,
                            responce.P);
                        break;

                    case BotResponceType.MessageToAdmins:
                        await _entryAdmin.WaitForMessageToAdminsAsync(messageChat, user);
                        break;

                    case BotResponceType.ReplyToUser:
                        await _entryAdmin.WaitForMessageToUsersAsync(messageChat, user,
                            long.Parse(responce.P));
                        break;

                    case BotResponceType.SwitchNotifications:
                        await _entryAdmin.SwitchNotificationsToUserAsync(messageChat, user,
                            bool.Parse(responce.P));
                        break;
                    
                    case BotResponceType.None:
                        _logger.LogWarning("Incorrect command received {@RespCommand}", responce);
                        break;
                    
                    default:
                        _logger.LogWarning("Incorrect command received {@RespCommand}", responce);
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
