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

    public async Task FillLatestRequestAsync(User user)
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
            UserId = user.Id,
            IsCompleted = false,
            TimeStamp = DateTime.Now
        };

        _repo.UpdateRefRequest(request);

        await IterateRequestAsync(user.Id, request);
    }

    private async Task IterateRequestAsync(ChatId chatId, RefRequest refRequest)
    {
        var unanswered = _questionnaire
            .Entries
            .Where(z => z.Category.Equals(refRequest.CurrentCategory))
            .Select(z => z.Text)
            .Except(refRequest.Answers.Select(z => z.Question))
            .FirstOrDefault();

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
            
            var buttons = menu
                .Select(z => new[]
                {
                    InlineKeyboardButton.WithCallbackData(z!, BotResponce.Create("q_move", z)),
                }).ToList();

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Завершить заполнение", BotResponce.Create("q_finish")),
                InlineKeyboardButton.WithCallbackData("Главное меню", BotResponce.Create("q_return")),
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

        var unanswered = _questionnaire
            .Entries
            .Where(z => z.Category.Equals(refRequest.CurrentCategory))
            .Select(z => z.Text)
            .Except(refRequest.Answers.Select(z => z.Question))
            .FirstOrDefault();

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

    public async Task CompleteAsync(ChatId messageChat, User user)
    {
        if (!_repo.TryGetActiveUserRequest(user.Id, out var refRequest))
        {
            return;
        }

        refRequest.IsCompleted = true;

        _repo.UpdateRefRequest(refRequest);

        if (refRequest.Answers.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: messageChat,
                parseMode: ParseMode.MarkdownV2,
                text:
                $"Завершенная анкета\r\n{string.Join("\r\n", refRequest.Answers.Select(z => $"`{z.Question.PadRight(20).Substring(0, 20)}|\t`{z.Answer}"))}",
                disableWebPagePreview: false
            );
        }

        _logger.LogInformation("Ref request {RefId} completed by {UserId}", refRequest.Id, user.Id);
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
                parseMode: ParseMode.MarkdownV2,
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
}