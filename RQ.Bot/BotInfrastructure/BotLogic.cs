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
            
            if (await _entryAdmin.IsUserReply(message.Chat.Id, user.Id, message.Text!))
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
            var usage = $"Ваш уровень доступа: {(isUserAdmin ? "*администраторский*" : "пользовательский")}\r\n";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Новая анкета", BotResponce.Create("fill_request")),
                InlineKeyboardButton.WithCallbackData("Написать администраторам", BotResponce.Create("message_to_admins")),
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
                var user = callbackQuery.From;

                var responce = BotResponce.FromString(callbackQuery.Data!);

                _logger.LogInformation("Received callback {Callback}: {Payload}", responce.E, responce.P);

                switch (responce.E)
                {
                    case "all_user_queries":
                        await _entryQuestionnaire.GetUserRefRequestAsync(callbackQuery.Message?.Chat!, user);

                        break;
                        
                    case "fill_request":
                        await _entryQuestionnaire.FillLatestRequestAsync(callbackQuery.Message?.Chat, user);

                        break;
                        
                    case "auq":
                        await _entryQuestionnaire.ShowArchiveRequestAsync(callbackQuery.Message?.Chat!, user,
                            Guid.Parse(responce.P));
                        break;
                    
                    case "add_permitions":
                        await _entryAdmin.PromoteUserAsync(user.Id, long.Parse(responce.P));
                        break;
                    
                    case "get_current_csv":
                        await _entryDownloadCsv.GetRequestsInCsvAsync(callbackQuery.Message?.Chat!, false);
                        break;
                    
                    case "get_all_csv":
                        await _entryDownloadCsv.GetRequestsInCsvAsync(callbackQuery.Message?.Chat!, true);
                        break;
                    
                    case "get_current_xlsx":
                        await _entryDownloadCsv.GetRequestsInXlsxAsync(callbackQuery.Message?.Chat!, false);
                        break;
                    
                    case "get_all_xlsx":
                        await _entryDownloadCsv.GetRequestsInXlsxAsync(callbackQuery.Message?.Chat!, true);
                        break;
                        
                    case "archive":
                        await _entryAdmin.ArchiveAsync(callbackQuery.Message?.Chat!, user);
                        break;
                    
                    case "q_finish":
                        await _entryQuestionnaire.CompleteAsync(callbackQuery.Message?.Chat!, user.Id);
                        await Usage(callbackQuery.Message);
                        break;
                    
                    case "q_return":
                        await _entryQuestionnaire.ReturnToRootAsync(callbackQuery.Message?.Chat!, user.Id);
                        break;
                        
                    case "q_move":
                        await _entryQuestionnaire.MoveMenuAsync(callbackQuery.Message?.Chat!, user, responce.P);
                        break;
                        
                    case "q_rem":
                        await _entryQuestionnaire.RemoveAnswersForCategoryAsync(callbackQuery.Message?.Chat!, user, responce.P);
                        break;
                    
                    case "list_admins":
                        await _entryAdmin.ListAdminsApprovedByUsersAsync(callbackQuery.Message?.Chat!, user);
                        break;
                        
                    case "remove_user":
                        await _entryAdmin.RevokeAdminAsync(callbackQuery.Message?.Chat!, long.Parse(responce.P));
                        break;
                    case "message_to_admins":
                        await _entryAdmin.WaitForMessageToAdminsAsync(callbackQuery.Message?.Chat!, user);
                        break;
                    
                    case "reply_to_user":
                        await _entryAdmin.WaitForMessageToUsersAsync(callbackQuery.Message?.Chat!, user,
                            long.Parse(responce.P));
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
