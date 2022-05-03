using System.Globalization;
using System.Text;
using Bot.Repo;
using CsvHelper;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
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

    public async Task GetRequestsInCsv(ChatId chatId, bool allRequests)
    {
        var dataToRenderCsv = allRequests ? _repo.GetAllRequests() : _repo.GetCurrentRequests();

        var sb = RenderCsv(dataToRenderCsv.OrderByDescending(z=>z.TimeStamp));
        
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms, new UTF8Encoding(true));
        await sw.WriteAsync(sb);
        await sw.FlushAsync();
        ms.Position = 0;

        var payload = new InputOnlineFile(ms, $"{(allRequests ? "ВСЕ" : "ТЕКУЩИЕ")}_{DateTime.Now.Ticks}_dataset.csv");

        await _botClient.SendDocumentAsync(
            disableContentTypeDetection: true,
            document: payload,
            chatId: chatId,
            caption: $"Выгрузка *{(allRequests ? "всех" : "текущих")}* запросов на {DateTime.Now}",
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

    public async Task GetRequestsInXlsx(ChatId chatId, bool allRequests)
    {
        var dataToRenderXlsx = allRequests ? _repo.GetAllRequests() : _repo.GetCurrentRequests();

        var ms = await RenderXlsxAsync(dataToRenderXlsx.OrderByDescending(z=>z.TimeStamp));
        
        var payload = new InputOnlineFile(ms, $"{(allRequests ? "ВСЕ" : "ТЕКУЩИЕ")}_{DateTime.Now.Ticks}_dataset.xlsx");
        
        
        await _botClient.SendDocumentAsync(
            disableContentTypeDetection: true,
            document: payload,
            chatId: chatId,
            caption: $"Выгрузка *{(allRequests ? "всех" : "текущих")}* запросов на {DateTime.Now}",
            parseMode: ParseMode.Markdown
        );

    }

    private async Task<Stream> RenderXlsxAsync(IEnumerable<RefRequest> dataToRenderXlsx)
    {
        var records = new List<Dictionary<string, string>>();
        var users = _repo.GetAllUsers()
            .ToDictionary(z => z.UserId, z => z);

        foreach (var refRequest in dataToRenderXlsx)
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

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var ms = new MemoryStream();
        var package = new ExcelPackage(ms);
        var sheet = package.Workbook.Worksheets.Add("Заявки");

        var headings = records.SelectMany(z => z.Keys).Distinct().ToArray();
        var col = 0;
        var row = 1;
        foreach (var heading in headings)
        {
            sheet.Cells[row, ++col].Style.Font.Bold = true;
            sheet.Cells[row, col].Value = heading;
            sheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thick);
            sheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[row, col].AutoFitColumns();
        }

        foreach (var item in records)
        {
            col = 0;
            row++;
            foreach (var heading in headings)
            {
                sheet.Cells[row, ++col].Style.Font.Bold = false;
                sheet.Cells[row, col].Value = item.TryGetValue(heading, out var value) ? value : "-";
            }
        }

        await package.SaveAsync();
        ms.Position = 0;
        return ms;
    }
}