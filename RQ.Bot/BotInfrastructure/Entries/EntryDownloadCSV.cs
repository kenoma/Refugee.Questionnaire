using System.Globalization;
using System.Text;
using Bot.Repo;
using CsvHelper;
using RQ.DTO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace RQ.Bot.BotInfrastructure.Entry;

public class EntryDownloadCsv
{
    private readonly TelegramBotClient _botClient;
    private readonly IRepository _repo;

    public EntryDownloadCsv(TelegramBotClient botClient, IRepository repo)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public async Task GetUsersRequests(ChatId chatId, long userId = 0)
    {
        var dataToRenderCsv = userId == 0 ? _repo.GetAllRequest() : _repo.GetAllRequestFromUser(userId);

        var sb = RenderCsv(dataToRenderCsv);
        
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms, new UTF8Encoding(true));
        await sw.WriteAsync(sb);
        await sw.FlushAsync();
        ms.Position = 0;

        var payload = new InputOnlineFile(ms, $"{userId}_{DateTime.Now.Ticks}_dataset.csv");

        await _botClient.SendDocumentAsync(
            disableContentTypeDetection: true,
            document: payload,
            chatId: chatId,
            caption: $"Выгрузка запросов `{(userId == 0 ? "всех" : "пользователя")}` на {DateTime.Now}",
            parseMode: ParseMode.Markdown
        );
    }

    private StringBuilder RenderCsv(IEnumerable<RefRequest> dataToRenderCsv)
    {
        var records = new List<Dictionary<string, string>>();
        var users = _repo.GetAllUsers()
            .ToDictionary(z => z.UserId, z => z);
        
        foreach (var refRequest in dataToRenderCsv)
        {
            var record = new Dictionary<string, string>();
            record.TryAdd("Дата заполнения",
                refRequest.TimeStamp.ToString("dd:MM:yyyy hh:mm", CultureInfo.InvariantCulture));

            if (users.TryGetValue(refRequest.UserId, out var user))
            {
                record.TryAdd("Telegram", $"@{user.Username}");
            }

            foreach (var answ in refRequest.Answers)
            {
                record.Add(answ.Question, answ.Answer);
            }

            records.Add(record);
        }

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var headings = records.SelectMany(z => z.Keys).Distinct().ToArray();

        foreach (var heading in headings)
        {
            csv.WriteField(heading);
        }

        csv.NextRecord();

        foreach (var item in records)
        {
            foreach (var heading in headings)
            {
                csv.WriteField(item.TryGetValue(heading, out var value) ? value : "-");
            }

            csv.NextRecord();
        }

        return writer.GetStringBuilder();
    }
}