using System.Text.RegularExpressions;
using Bot.Repo;
using RQ.DTO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RQ.Bot.BotInfrastructure.Entry;

public class EntryQuestionnaire
{
    private readonly TelegramBotClient _botClient;
    private readonly IRepository _repo;
    private readonly Questionnaire _questionnaire;
    private readonly ILogger<EntryQuestionnaire> _logger;
    private const int ButtonsPerMessage = 30;
    public const string CategoriesSeparator = "->";

    public EntryQuestionnaire(TelegramBotClient botClient, IRepository repo, Questionnaire questionnaire,
        ILogger<EntryQuestionnaire> logger)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _questionnaire = questionnaire ?? throw new ArgumentNullException(nameof(questionnaire));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task GetUserRefRequestAsync(Chat chatId, User user)
    {
        if (user == null)
            return;

        if (chatId == null)
            return;

        if (_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: "Необходимо завершить заполнение активного запроса, прежде чем продолжить",
                disableWebPagePreview: false
            );

            await IterateRequestAsync(chatId, refRequest);
            return;
        }

        var allRequests = _repo.GetAllRequestFromUser(user.Id)
            .OrderBy(z => z.TimeStamp)
            .ToArray();

        for (var skip = 0; skip < ButtonsPerMessage; skip += ButtonsPerMessage)
        {
            var buttons = allRequests
                .Skip(skip)
                .Take(ButtonsPerMessage)
                .Select(z => new[]
                {
                    InlineKeyboardButton.WithCallbackData(z.ToString(), BotResponce.Create("auq", z.Id)),
                });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Markdown,
                text: $"{skip} - {skip + ButtonsPerMessage}",
                replyMarkup: new InlineKeyboardMarkup(buttons),
                disableWebPagePreview: false
            );
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            parseMode: ParseMode.Markdown,
            text:
            $"Всего {allRequests.Length} заявок за период {allRequests.Select(z => z.TimeStamp).DefaultIfEmpty(DateTime.Now).Min()} - {allRequests.Select(z => z.TimeStamp).DefaultIfEmpty(DateTime.Now).Max()}",
            disableWebPagePreview: false
        );
    }

    public async Task ShowArchiveRequestAsync(Chat chatId, User user, Guid requestId)
    {
        if (user == null)
            return;

        if (chatId == null)
            return;

        if (_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: "Необходимо завершить заполнение активного запроса, прежде чем продолжить",
                disableWebPagePreview: false
            );

            await IterateRequestAsync(chatId, refRequest);
            return;
        }

        var request = _repo.GetRequest(requestId);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            parseMode: ParseMode.Html,
            text: string.Join("\r\n", request.Answers.Select(z => $"<b>{z.Question}</b>:{z.Answer}")),
            disableWebPagePreview: false
        );
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
                text: "Необходимо завершить заполнение активного запроса, прежде чем продолжить",
                disableWebPagePreview: false
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

        await IterateRequestAsync(user.Id, request);
    }

    private async Task IterateRequestAsync(ChatId chatId, RefRequest refRequest)
    {
        if (await PollStage(refRequest, chatId))
            return;
        
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

        _logger.LogInformation("IterateRequestAsync {ChatId} proceed to {Unanswered}", chatId, unanswered);

        if (unanswered == null)
        {
            _repo.UpdateRefRequest(refRequest);

            var previewBody = $"Раздел {refRequest.CurrentCategory} заполнен";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: previewBody,
                disableWebPagePreview: false
            );
            await ReturnToRootAsync(chatId, refRequest.UserId);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Markdown,
                text: unanswered,
                disableWebPagePreview: false
            );
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
                text: "Некорректный ответ на вопрос",
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
                    InlineKeyboardButton.WithCallbackData(z!, BotResponce.Create("q_move", z)),
                }).ToList();

            buttons.AddRange(itemsToRemove.Select(z =>
            {
                return new[]
                {
                    InlineKeyboardButton.WithCallbackData($"Удалить записи: {z}", BotResponce.Create("q_rem", z))
                };
            }));

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Завершить", BotResponce.Create("q_finish")),
                InlineKeyboardButton.WithCallbackData("Начало", BotResponce.Create("q_return")),
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
        _repo.UpdateRefRequest(refRequest);
        _repo.RemoveRequest(refRequest.Id);

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
            await _botClient.SendTextMessageAsync(
                chatId: messageChat,
                parseMode: ParseMode.Markdown,
                text:
                $"Завершенная анкета\r\n{string.Join("\r\n", refRequest.Answers.Select(z => $"`{z.Question.PadRight(20).Substring(0, 20)}|\t`{z.Answer}"))}",
                disableWebPagePreview: false
            );
        }

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

        if (refRequest.Answers.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: messageChat,
                parseMode: ParseMode.Markdown,
                text:
                $"Заполненные данные\r\n{string.Join("\r\n", refRequest.Answers.Select(z => $"`{z.Question.PadRight(20).Substring(0, 20)}|\t`{z.Answer}"))}",
                disableWebPagePreview: false
            );
        }

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
            $"Записи раздела {catToRemove} и дочерние были удалены",
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
            question: "Выберите категории:",
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

        var activeSwitches = switches.Select(z => z.Text).Except(refRequest.Answers.Select(z => z.Question)).Take(9).ToArray();

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
}