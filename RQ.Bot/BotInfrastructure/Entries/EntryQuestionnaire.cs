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
    private const int ButtonsPerMessage = 30;

    public EntryQuestionnaire(TelegramBotClient botClient, IRepository repo, Questionnaire questionnaire)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _questionnaire = questionnaire ?? throw new ArgumentNullException(nameof(questionnaire));
    }

    public async Task StartQuestionnaireAsync(ChatId? chatId, User? user)
    {
        if (user == null)
            return;

        if (chatId == null)
            return;

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Все запросы", BotResponce.Create("all_user_queries")),
            InlineKeyboardButton.WithCallbackData("Новый запрос", BotResponce.Create("fill_request")),
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            parseMode: ParseMode.Markdown,
            text: "Что делаем?",
            replyMarkup: inlineKeyboard,
            disableWebPagePreview: false
        );
    }

    public async Task GetUserRefRequestAsync(Chat? chatId, User? user)
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

            IterateRequest(chatId, refRequest);
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

    public async Task ShowArchiveRequest(Chat? chatId, User? user, Guid requestId)
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

            IterateRequest(chatId, refRequest);
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

    public async Task FillLatestRequest(Chat chatId, User user)
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

            await IterateRequest(chatId, refRequest);
            return;
        }

        var request = new RefRequest
        {
            UserId = user.Id,
            IsCompleted = false,
            TimeStamp = DateTime.Now
        };

        _repo.UpdateRefRequest(request);

        await IterateRequest(chatId, request);
    }

    private async Task IterateRequest(ChatId chatId, RefRequest refRequest)
    {
        var unanswered = _questionnaire
            .Entries
            .Select(z => z.Text)
            .Except(refRequest.Answers.Select(z => z.Question))
            .FirstOrDefault();
        
        if (unanswered == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                parseMode: ParseMode.Html,
                text: string.Join("\r\n", refRequest.Answers.Select(z => $"<b>{z.Question}</b>:{z.Answer}")),
                disableWebPagePreview: false
            );    
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            parseMode: ParseMode.Markdown,
            text: unanswered,
            disableWebPagePreview: false
        );
    }

    public async Task<bool> TryProcessStateMachineAsync(ChatId chatId, long userId, string messageText)
    {
        if (!_repo.TryGetActiveUserRequest(userId, out var refRequest))
        {
            return false;
        }

        var unanswered = _questionnaire
            .Entries
            .Select(z => z.Text)
            .Except(refRequest.Answers.Select(z => z.Question))
            .FirstOrDefault();

        if (unanswered == null)
        {
            refRequest.IsCompleted = true;
            _repo.UpdateRefRequest(refRequest);
            await IterateRequest(chatId, refRequest);
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

        await IterateRequest(chatId, refRequest);
        return true;
    }
}