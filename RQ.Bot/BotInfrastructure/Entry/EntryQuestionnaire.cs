using System.Text.RegularExpressions;
using Bot.Repo;
using Newtonsoft.Json;
using RQ.Bot.Domain;
using RQ.Bot.Domain.Enum;
using RQ.Bot.Integrations;
using RQ.DTO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace RQ.Bot.BotInfrastructure.Entry;

public class EntryQuestionnaire
{
    private readonly TelegramBotClient _botClient;
    private readonly IRepository _repo;
    private readonly Questionnaire _questionnaire;
    private readonly ILogger<EntryQuestionnaire> _logger;
    private readonly IEnumerable<IBotIntegration> _integrations;
    private const string CategoriesSeparator = "->";

    public EntryQuestionnaire(TelegramBotClient botClient, IRepository repo, Questionnaire questionnaire,
        ILogger<EntryQuestionnaire> logger, IEnumerable<IBotIntegration> integrations)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _questionnaire = questionnaire ?? throw new ArgumentNullException(nameof(questionnaire));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _integrations = integrations ?? throw new ArgumentNullException(nameof(integrations));
    }

    public async Task FillLatestRequestAsync(ChatId chatId, User user)
    {
        if (user == null)
            return;

        if (_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
        {
            await _botClient.SendTextMessageAsync(
                chatId: user.Id,
                parseMode: ParseMode.Html,
                text: "Необходимо завершить заполнение активного запроса, прежде чем продолжить"
            );

            await IterateRequestAsync(user.Id, refRequest);
            return;
        }

        var request = new RefRequest
        {
            ChatId = chatId.Identifier ?? 0L,
            UserId = user.Id,
            IsCompleted = false,
            TimeStamp = DateTime.Now
        };

        _repo.UpdateRefRequest(request);

        foreach (var headliner in _questionnaire.Headliners)
        {
            await SendQuestMessageToUser(user.Id, headliner);
        }

        await IterateRequestAsync(user.Id, request);
    }

    private async Task IterateRequestAsync(ChatId chatId, RefRequest refRequest)
    {
        var answered = refRequest.Answers.Select(z => z.Question).ToHashSet();

        if (!_questionnaire.Entries.Select(z => z.Text).Except(answered).Any())
        {
            await CompleteAsync(chatId, refRequest.UserId);
            return;
        }

        var unanswered = _questionnaire
            .Entries
            .Where(z => string.IsNullOrWhiteSpace(z.Category) || z.Category.Equals(refRequest.CurrentCategory))
            .Select(z => z.Text)
            .Except(refRequest.Answers.Select(z => z.Question))
            .FirstOrDefault();

        var isZeroGroup = _questionnaire
            .Entries
            .Where(z => unanswered?.Contains(z.Text) ?? false)
            .Any(z => z.Group == 0);

        if (!isZeroGroup && await PollStage(refRequest, chatId))
            return;

        _logger.LogInformation("IterateRequestAsync {ChatId} proceed to {Unanswered}", chatId, unanswered);

        var correspondingQuest = _questionnaire.Entries.FirstOrDefault(z => z.Text == unanswered);

        if (correspondingQuest?.AutopassMode == AutopassMode.Simple)
        {
            await SendQuestMessageToUser(chatId, correspondingQuest);

            refRequest.Answers = refRequest.Answers.Concat(new[]
            {
                new RefRequestEntry
                {
                    Question = correspondingQuest.Text,
                    Answer = "✓"
                }
            }).ToArray();
            _repo.UpdateRefRequest(refRequest);

            await IterateRequestAsync(chatId, refRequest);
        }
        else
        {
            if (unanswered == null)
            {
                _repo.UpdateRefRequest(refRequest);
                
                await ReturnToRootAsync(chatId, refRequest.UserId);
            }
            else
            {
                await SendQuestMessageToUser(chatId, correspondingQuest);
            }
        }
    }
            record CallbackData(BotResponseType ResponseType, string Response);

    /// <summary>
    /// Отправить текстовое сообщение.
    /// <remarks>
    /// Если текстовое сообщение содержит варианты ответов, то они также будут отрисованны.
    /// </remarks>
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="entry">Вопрос.</param>
    private async Task SendTextMessage(ChatId chatId, QuestionnaireEntry entry)
    {
        if (entry.PossibleResponses.Any())
        {
            var buttons = entry.PossibleResponses
                .Select(response =>
                {
                    var callbackData = BotResponse.Create(BotResponseType.PossibleResponses, response);
                    
                    var button = InlineKeyboardButton.WithCallbackData(response, callbackData);
                    return button;
                });


            var replyKeyboardMarkup = new InlineKeyboardMarkup(buttons);

            await _botClient.SendTextMessageAsync(chatId, entry.Text, replyMarkup: replyKeyboardMarkup);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Markdown,
                text: entry.Text,
                disableWebPagePreview: false
            );
        }
    }

    private async Task SendQuestMessageToUser(ChatId chatId, QuestionnaireEntry questEntry)
    {
        if (string.IsNullOrEmpty(questEntry.Attachment))
        {
            await SendTextMessage(chatId, questEntry);
        }
        else
        {
            if (!File.Exists(questEntry.Attachment))
            {
                await _botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                await _botClient.SendPhotoAsync(
                    chatId,
                    new InputOnlineFile(questEntry.Attachment),
                    caption: questEntry.Text);
            }
            else
            {
                var imageExtensions = new HashSet<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };

                if (imageExtensions.Contains(Path.GetExtension(questEntry.Attachment!)?.ToUpperInvariant()))
                {
                    await _botClient.SendPhotoAsync(
                        chatId,
                        new InputOnlineFile(File.OpenRead(questEntry.Attachment!)),
                        caption: questEntry.Text);
                }
                else
                {
                    await _botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    await _botClient.SendVideoAsync(
                        chatId,
                        new InputOnlineFile(File.OpenRead(questEntry.Attachment!)),
                        caption: questEntry.Text);
                }
            }
        }
    }

    public async Task<bool> TryProcessStateMachineAsync(ChatId chatId, long userId, string messageText)
    {
        if (!_repo.TryGetActiveUserRequest(userId, out var refRequest))
        {
            return false;
        }

        var answered = refRequest.Answers.Select(z => z.Question).ToHashSet();
        if (!_questionnaire.Entries.Select(z => z.Text).Except(answered).Any())
        {
            await CompleteAsync(chatId, refRequest.UserId);
            return true;
        }

        var unanswered = _questionnaire
            .Entries
            .Where(z => string.IsNullOrWhiteSpace(z.Category) || z.Category.Equals(refRequest.CurrentCategory))
            .Select(z => z.Text)
            .Except(refRequest.Answers.Select(z => z.Question))
            .FirstOrDefault();

        _logger.LogInformation("TryProcessStateMachineAsync {UserId} {Unanswered}", userId, unanswered);

        var correspondingEntry = _questionnaire.Entries.FirstOrDefault(z => z.Text == unanswered);

        if ((string.IsNullOrEmpty(unanswered) ||
             !string.IsNullOrWhiteSpace(correspondingEntry?.Category))
            && await DrawCategories(chatId, refRequest))
        {
            return true;
        }

        if (unanswered == null || string.IsNullOrWhiteSpace(messageText))
        {
            _repo.UpdateRefRequest(refRequest);
            await IterateRequestAsync(chatId, refRequest);
            return true;
        }

        var entry = _questionnaire.Entries.First(z => z.Text == unanswered);

        if (string.IsNullOrWhiteSpace(entry.ValidationRegex) || Regex.IsMatch(messageText, entry.ValidationRegex))
        {
            refRequest.Answers = refRequest.Answers.Concat(new[]
            {
                new RefRequestEntry
                {
                    Question = entry.Text,
                    Answer = messageText
                }
            }).ToArray();
            _repo.UpdateRefRequest(refRequest);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Markdown,
                text: "*⚠⚠⚠ Некорректный ответ на вопрос ⚠⚠⚠*",
                disableWebPagePreview: false
            );
        }

        await IterateRequestAsync(chatId, refRequest);
        return true;
    }


    private async Task<bool> DrawCategories(ChatId chatId, RefRequest refRequest)
    {
        var leafs = _questionnaire
            .Entries
            .Where(z => z.Category.Equals(refRequest.CurrentCategory))
            .ToArray();

        if (!leafs.Any())
        {
            var answered = refRequest.Answers.Select(z => z.Question).ToHashSet();

            var menu = _questionnaire
                .Entries
                .Where(z => !answered.Contains(z.Text))
                .Where(z => z.Category.StartsWith(refRequest.CurrentCategory ?? string.Empty))
                .Select(z => (string.IsNullOrWhiteSpace(refRequest.CurrentCategory)
                        ? z.Category
                        : z.Category.Replace(refRequest.CurrentCategory, ""))
                    .Split(CategoriesSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault())
                .AsEnumerable()
                .Distinct()
                .ToArray();

            var itemsToRemove = _questionnaire
                .Entries
                .Where(z => !string.IsNullOrWhiteSpace(z.Category))
                .Where(z => answered.Contains(z.Text))
                .Where(z => z.Category.StartsWith(refRequest.CurrentCategory ?? string.Empty))
                .Select(z => (string.IsNullOrWhiteSpace(refRequest.CurrentCategory)
                        ? z.Category
                        : z.Category.Replace(refRequest.CurrentCategory, ""))
                    .Split(CategoriesSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault())
                .AsEnumerable()
                .Distinct()
                .ToArray();

            var buttons = menu
                .Select(z => new[]
                {
                    InlineKeyboardButton.WithCallbackData(z!, BotResponse.Create(BotResponseType.QMove, z)),
                }).ToList();

            buttons.AddRange(itemsToRemove.Select(z =>
            {
                return new[]
                {
                    InlineKeyboardButton.WithCallbackData($"Перезаполнить: {z}",
                        BotResponse.Create(BotResponseType.QRem, z))

                };
            }));
            
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Завершить", BotResponse.Create(BotResponseType.QFinish)),
                InlineKeyboardButton.WithCallbackData("Обратно", BotResponse.Create(BotResponseType.QReturn)),
            });

            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    parseMode: ParseMode.Html,
                    text: $"Выберите пункт меню",
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    disableWebPagePreview: false
                );
            }
            catch
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    parseMode: ParseMode.Markdown,
                    text:
                    $"При генерации пунктов меню: `{string.Join(",", menu)}` возникла ошибка, попробуйте сократить их длину (суммарная длина нагрузки не должна быть более 30 символов)",
                    disableWebPagePreview: false
                );
                throw;
            }

            return true;
        }

        return false;
    }

    public async Task InterruptCurrentQuest(long chatId, long userId)
    {
        if (!_repo.TryGetActiveUserRequest(userId, out var refRequest))
        {
            return;
        }

        refRequest.IsCompleted = true;
        refRequest.IsInterrupted = true;
        _repo.UpdateRefRequest(refRequest);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            parseMode: ParseMode.Markdown,
            text: "*Заполнение анкеты прервано*",
            disableWebPagePreview: false
        );
    }

    public async Task CompleteAsync(ChatId messageChat, long userId)
    {
        if (!_repo.TryGetActiveUserRequest(userId, out var refRequest))
        {
            return;
        }

        refRequest.IsCompleted = true;

        _repo.UpdateRefRequest(refRequest);

        if (refRequest.Answers.Any())
        {
            var questReview =
                $"Анкета:\r\n{string.Join("\r\n", refRequest.Answers.Select(z => $"`{z.Question.PadRight(20).Substring(0, 20)}|\t`{z.Answer}"))}";

            await _botClient.SendTextMessageAsync(
                chatId: messageChat,
                parseMode: ParseMode.Markdown,
                text:
                questReview,
                disableWebPagePreview: false
            );

            foreach (var finisher in _questionnaire.Finishers)
            {
                await SendQuestMessageToUser(messageChat, finisher);
            }

            foreach (var admin in _repo.GetAdminUsers().Where(z => z.IsNotificationsOn))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: admin.ChatId,
                    parseMode: ParseMode.Markdown,
                    text: $"*Новая анкета*\r\n{questReview}",
                    disableWebPagePreview: false
                );
            }
        }

        await Task.WhenAll(_integrations.Select(z => z.PushRequestToIntegrationAsync(refRequest)));

        _logger.LogInformation("Ref request {RefId} completed by {UserId}", refRequest.Id, userId);
    }

    public async Task ReturnToRootAsync(ChatId messageChat, long userId)
    {
        if (!_repo.TryGetActiveUserRequest(userId, out var refRequest))
        {
            return;
        }

        refRequest.CurrentCategory = string.Empty;

        _repo.UpdateRefRequest(refRequest);
        _logger.LogInformation("Ref request {RefId} backed to root {UserId}", refRequest.Id, userId);
        
        await TryProcessStateMachineAsync(messageChat, userId, string.Empty);
    }

    public async Task MoveMenuAsync(ChatId messageChat, User user, string responcePayload)
    {
        if (!_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
        {
            return;
        }

        refRequest.CurrentCategory = string.IsNullOrWhiteSpace(refRequest.CurrentCategory)
            ? responcePayload
            : $"{refRequest.CurrentCategory}{CategoriesSeparator}{responcePayload}";

        _repo.UpdateRefRequest(refRequest);
        _logger.LogInformation("Ref request {RefId} moved to {Category}", refRequest.Id, refRequest.CurrentCategory);
        await TryProcessStateMachineAsync(messageChat, user.Id, string.Empty);
    }

    public async Task RemoveAnswersForCategoryAsync(ChatId messageChat, User user, string responcePayload)
    {
        if (!_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
        {
            return;
        }

        var catToRemove = string.IsNullOrWhiteSpace(refRequest.CurrentCategory)
            ? responcePayload
            : $"{refRequest.CurrentCategory}{CategoriesSeparator}{responcePayload}";

        var questions = _questionnaire.Entries
            .Where(z => z.Category.StartsWith(catToRemove ?? string.Empty))
            .Select(z => z.Text)
            .ToHashSet();

        refRequest.Answers = refRequest.Answers.Where(z => !questions.Contains(z.Question)).ToArray();

        _repo.UpdateRefRequest(refRequest);
        _logger.LogInformation("Ref request {RefId} category {Category} removed", refRequest.Id, catToRemove);
        await _botClient.SendTextMessageAsync(
            chatId: messageChat,
            parseMode: ParseMode.Markdown,
            text:
            $"Записи раздела {catToRemove} и дочерние были удалены, теперь вы можете снова их заполнить",
            disableWebPagePreview: false
        );
        await TryProcessStateMachineAsync(messageChat, user.Id, string.Empty);
    }

    private async Task<bool> PollStage(RefRequest refRequest, ChatId chatId)
    {
        var switches = _questionnaire.Entries.Where(z => z.IsGroupSwitch != 0).ToArray();

        if (!switches.Any())
            return false;

        var actualSwitches = switches.Select(z => z.Text).Except(refRequest.Answers.Select(z => z.Question)).ToArray();

        if (!actualSwitches.Any())
            return false;

        var pollQuestions = actualSwitches
            .Take(9)
            .Select(z => string.Concat(z.Take(100)))
            .Concat(new[] { "Ничего из вышеперечисленного" })
            .ToArray();

        await _botClient.SendPollAsync(
            chatId: chatId,
            question:
            "Ответьте, пожалуйста, на следующие вопросы (если да -  поставьте галочку в соответствующих пунктах, а затем нажмите на кнопку \"Голосовать\" / \"Vote\"):",
            pollQuestions,
            allowsMultipleAnswers: true,
            isAnonymous: false
        );

        return true;
    }

    public async Task ProcessPoll(PollAnswer updatePollAnswer)
    {
        var user = updatePollAnswer.User;

        if (!_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
            return;

        var switches = _questionnaire.Entries.Where(z => z.IsGroupSwitch != 0).ToArray();

        var activeSwitches = switches.Select(z => z.Text).Except(refRequest.Answers.Select(z => z.Question)).Take(9)
            .ToArray();

        for (var switchIndex = 0; switchIndex < activeSwitches.Length; switchIndex++)
        {
            var s = activeSwitches[switchIndex];
            if (!updatePollAnswer.OptionIds.Contains(9) && updatePollAnswer.OptionIds.Contains(switchIndex))
            {
                refRequest.Answers = refRequest.Answers.Concat(new[]
                {
                    new RefRequestEntry
                    {
                        Question = s,
                        Answer = "✓"
                    }
                }).ToArray();
            }
            else
            {
                var group = switches.First(z => z.Text == s).Group;

                foreach (var entry in _questionnaire.Entries.Where(z => z.Group == group))
                {
                    refRequest.Answers = refRequest.Answers.Concat(new[]
                    {
                        new RefRequestEntry
                        {
                            Question = entry.Text,
                            Answer = "-"
                        }
                    }).ToArray();
                }
            }
        }

        _repo.UpdateRefRequest(refRequest);

        await IterateRequestAsync(refRequest.ChatId, refRequest);
    }

    public RefRequest[] GetAllUserRequest(User msgFrom)
    {
        return _repo.GetAllRequestFromUser(msgFrom.Id)
            .OrderByDescending(z => z.TimeStamp)
            .ToArray();
    }
}