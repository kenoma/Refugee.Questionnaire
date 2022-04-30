using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CvLab.TelegramBot.Impl.Entry;
using Prometheus;
using Serilog;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace CvLab.TelegramBot.Impl
{
    /// <inheritdoc />
    internal class BotLogic : IUpdateHandler
    {
        private readonly TelegramBotClient _bot;
        private readonly EntryConfigureChat _entryConfigureChat;
        
        private static readonly Counter _commandsCount = Metrics.CreateCounter("commands_total", "Количество команд, отработанных ботом");

        /// <summary>
        ///
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="entryProjectsAndMilestoneSelect"></param>
        /// <param name="entryConfigureChat"></param>
        /// <param name="entryIssues"></param>
        /// <param name="entryIssueCreate"></param>
        /// <param name="reportPublisher"></param>
        public BotLogic(
            TelegramBotClient botClient,
            EntryConfigureChat entryConfigureChat
        )
        {
            _bot                             = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _entryConfigureChat              = entryConfigureChat ?? throw new ArgumentNullException(nameof(entryConfigureChat));
        }

        /// <inheritdoc />
        public async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var handler = update.Type switch
                {
                    UpdateType.Message            => BotOnMessageReceived(update.Message),
                    UpdateType.EditedMessage      => BotOnMessageReceived(update.Message),
                    UpdateType.CallbackQuery      => BotOnCallbackQueryReceived(update.CallbackQuery),
                    UpdateType.InlineQuery        => BotOnInlineQueryReceived(update.InlineQuery),
                    UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult),
                    UpdateType.PollAnswer         => BotOnPollAnswer(update.PollAnswer),
                    // UpdateType.Unknown:
                    // UpdateType.ChannelPost:
                    // UpdateType.EditedChannelPost:
                    // UpdateType.ShippingQuery:
                    // UpdateType.PreCheckoutQuery:
                    // UpdateType.Poll:
                    _ => UnknownUpdateHandlerAsync(update)
                };

                await handler;
                _commandsCount.Inc();
            }
            catch (Exception exception)
            {
                await HandleError(botClient, exception, cancellationToken);
            }
        }

        private async Task BotOnPollAnswer(PollAnswer updatePollAnswer)
        {
            Log.Information("New vote {@Vote}", updatePollAnswer);
        }

        /// <inheritdoc />
        public Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _                                       => exception.ToString()
            };

            Log.Error("Bot error {ErrorMessage}", errorMessage);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public UpdateType[]? AllowedUpdates { get; }

        private async Task BotOnMessageReceived(Message message)
        {
            var user       = message.From;
            var chatMember = await _bot.GetChatMemberAsync(message.Chat.Id, user.Id);

            if (chatMember.Status != ChatMemberStatus.Administrator && chatMember.Status != ChatMemberStatus.Creator)
            {
                var      r  = new Random(Environment.TickCount);
                string[] a1 = { "Товарищ!", "С другой стороны ", "Равным образом ", "Не следует, однако, забыть, что ", "Таким образом ", "Повседневная практика показыват, что ", "Значимость этих проблем настолько очевидная, что ", "Разнообразный и богатый опыт ", "Задача организации, в особенности же ", "Идейные соображения высокого порядка, а также " };
                string[] a2 = { "реализация намеченных плановых заданий ", "рамки и место обучения кадров ", "постоянный количественный рост и сфера нашей активности ", "сложившаеся структура организации ", "новая модель организационной деятельности ", "дальнейшее развитие различных форм деятельности ", "постоянное информационно-пропагандисткое обеспечение нашей деятельности ", "управление и развитие структуры ", "консультация с широким активом ", "начало повседневной работы по формированию позиции" };
                string[] a3 = { "играет важную роль в формировании ", "требует от наc анализа ", "требует определения и уточнения ", "способствует подготовке и реализации ", "обеспечивает широкому кругу ", "участие в формировании ", "в значительной степени обуславливает создание ", "позволяет оценить значение, представляет собой интересный эксперимент ", "позволяет выполнять разные задачи ", "проверки влечет за собой интересный процесс внедрения и модернизации" };
                string[] a4 = { "существующим финансовых и административных условий.", "дальнейших направлений развития.", "системы массового участия.", "позиций, занимаемых участниками в отношении поставленных задач.", "новых предложений.", "направлений прогрессивного развития.", "системы обучения кадров, соответствующей насущным потребностям.", "соответствующих условий активизации.", "модели развития.", "форм воздействия." };

                await _bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"@{user.Username}! {(a1[r.Next(a1.Length)])+ (a2[r.Next(a2.Length)])+ (a3[r.Next(a3.Length)])+ (a4[r.Next(a4.Length)])}");

                return;
            }

            Log.Information("Receive message type: {MessageType}: {MessageText}", message.Type, message.Text);

            if (message.Type != MessageType.Text)
                return;

            if(string.IsNullOrEmpty(message.Text))
                return;

            if (message.Entities?.All(z => z.Type != MessageEntityType.BotCommand) ?? true)
                return;

            for (var entity = 0; entity < message.Entities.Length; entity++)
            {
                if(message.Entities[entity].Type!= MessageEntityType.BotCommand)
                    continue;

                Log.Information("Recognized bot command: {Command}", message.EntityValues.ElementAt(entity));

                var action = (message.EntityValues.ElementAt(entity).Split(new[] { " ", "@" }, StringSplitOptions.RemoveEmptyEntries).First()) switch
                {
                    "/s" => _entryConfigureChat.SendConfiguration(message),
                    _    => Usage(message)
                };

                await action;

                async Task Usage(Message msg)
                {
                    const string usage = "*Что мы с тобой можем сделать:*\r\n" +
                        "/p - Выбор майлстоуна\r\n" +
                        "/i - Выбор задачи для голосования\r\n" +
                        "/s <host|token> <text> - Конфигурация бота\r\n"+
                        "/o <text> - Создать новое ишью\r\n"+
                        "/c - Выгрузить список задач по текущему спринту в csv\r\n"+
                        "/r - Отчет по спринту";
                    

                    await _bot.SendTextMessageAsync(
                        chatId: msg.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: usage,
                        replyMarkup: new ReplyKeyboardRemove()
                    );
                }
            }
        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            try
            {
                var user       = callbackQuery.From;
                var chatMember = await _bot.GetChatMemberAsync(callbackQuery.Message.Chat.Id, user.Id);

                if (chatMember.Status != ChatMemberStatus.Administrator && chatMember.Status != ChatMemberStatus.Creator)
                {
                    await _bot.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: $"@{user.Username} не хулигань!");

                    return;
                }

                var responce = BotResponce.FromString(callbackQuery.Data);

                Log.Information("Received callback {@Callback}", responce);

                switch (responce.Entry)
                {
                    case "project":
                        //await _entryProjectsAndMilestoneSelect.ProcessProjectResponceAsync(callbackQuery, responce);

                        break;

                    case "milestone":
                        //await _entryProjectsAndMilestoneSelect.ProcessMilestoneResponceAsync(callbackQuery, responce);

                        break;

                    case "issues":
                        //await _entryIssues.SelectIssueToVote(callbackQuery.Message);

                        break;

                    case "issue":
                        //await _entryIssues.ProcessIssue(callbackQuery, responce);

                        break;

                    case "vote":
                        //await _entryIssues.StopVote(callbackQuery, responce);

                        break;
                }
            }
            catch (Exception e)
            {
                await HandleError(_bot, e, CancellationToken.None);
            }
        }

        private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
        {
            Log.Information($"Received inline query from: {inlineQuery.From.Id}");

            // InlineQueryResultBase[] results =
            // {
            //     // displayed result
            //     new InlineQueryResultArticle(
            //         id: "3",
            //         title: "TgBots",
            //         inputMessageContent: new InputTextMessageContent("hello")
            //     )
            // };
            //
            // await _bot.AnswerInlineQueryAsync(
            //     inlineQuery.Id,
            //     results,
            //     isPersonal: true,
            //     cacheTime: 0
            // );
        }

        private static Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
        {
            Log.Information("Received inline result: {ChosenInlineResultResultId}", chosenInlineResult.ResultId);
            return Task.CompletedTask;
        }

        private static Task UnknownUpdateHandlerAsync(Update update)
        {
            Log.Warning("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
