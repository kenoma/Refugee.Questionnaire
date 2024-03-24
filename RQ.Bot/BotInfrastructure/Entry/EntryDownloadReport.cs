﻿using System.Drawing;
using System.Globalization;
using System.Text;
using Bot.Repo;
using CsvHelper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RQ.Bot.Domain;
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
    private readonly Questionnaire _questionnaire;
    private readonly ReportGenerationParams _reportGenerationParams;

    public EntryDownloadCsv(TelegramBotClient botClient, IRepository repo, Questionnaire questionnaire,
        ReportGenerationParams reportGenerationParams)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _questionnaire = questionnaire ?? throw new ArgumentNullException(nameof(questionnaire));
        _reportGenerationParams =
            reportGenerationParams ?? throw new ArgumentNullException(nameof(reportGenerationParams));
    }

    public async Task GetRequestsInCsvAsync(ChatId chatId, bool allRequests, User user)
    {
        if (_repo.TryGetUserById(user.Id, out var userData) && !userData.IsAdministrator)
        {
            return;
        }
        
        await _botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
        
        var dataToRenderCsv = allRequests ? _repo.GetAllRequests() : _repo.GetCurrentRequests();

        var sb = RenderCsv(_reportGenerationParams.IsDescendingSorting? 
            dataToRenderCsv.Where(z => z.IsCompleted).OrderByDescending(z => z.TimeStamp.Ticks):
            dataToRenderCsv.Where(z => z.IsCompleted).OrderBy(z => z.TimeStamp.Ticks));

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
                refRequest.TimeStamp.ToString("dd.MM.yyyy hh:mm", CultureInfo.InvariantCulture));

            if (users.TryGetValue(refRequest.UserId, out var user))
            {
                record.TryAdd("Telegram", $"@{user.Username}");
            }

            foreach (var answ in refRequest.Answers)
                if (!record.TryAdd(answ.Question, answ.Answer))
                {
                    record[answ.Question] = $"{record[answ.Question]} answ.Answer";
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

    public async Task GetRequestsInXlsxAsync(ChatId chatId, bool allRequests, User user)
    {
        if (_repo.TryGetUserById(user.Id, out var userData) && !userData.IsAdministrator)
        {
            return;
        }

        await _botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
        
        var dataToRenderXlsx = allRequests ? _repo.GetAllRequests() : _repo.GetCurrentRequests();

        var ms = await RenderXlsxAsync(_reportGenerationParams.IsDescendingSorting
            ? dataToRenderXlsx.Where(z => z.IsCompleted).OrderByDescending(z => z.TimeStamp)
            : dataToRenderXlsx.Where(z => z.IsCompleted).OrderBy(z => z.TimeStamp));

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
        var records = ExtractDictionaryToRender(dataToRenderXlsx);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var ms = new MemoryStream();
        using var package = new ExcelPackage(ms);
        var sheet = package.Workbook.Worksheets.Add("Заявки");

        var headings = records.SelectMany(z => z.Keys).Distinct().ToArray();
        var col = 0;
        var row = 1;
        sheet.Cells[row, ++col].Style.Font.Bold = true;
        sheet.Cells[row, col].Value = "Номер";
        sheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thick);
        sheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        sheet.Cells[row, col].AutoFitColumns();
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
            sheet.Cells[row, ++col].Style.Font.Bold = true;
            sheet.Cells[row, col].Value = row-1;
            foreach (var heading in headings)
            {
                sheet.Cells[row, ++col].Style.Font.Bold = false;
                sheet.Cells[row, col].Value = item.TryGetValue(heading, out var value) ? value : "-";
            }
        }

        CheckDuplicates(headings, row, sheet);

        row = 0;
        foreach (var rec in records)
        {
            var recSheet = package.Workbook.Worksheets.Add($"{++row}");
            
            recSheet.Cells[1, 1].Style.Font.Bold = true;
            recSheet.Cells[1, 1].Value = "Номер заявки";
            recSheet.Cells[1, 2].Style.Font.Bold = true;
            recSheet.Cells[1, 2].Value = row;
            
            var rrow = 1;
            foreach (var kvpair in rec)
            {
                recSheet.Cells[++rrow, 1].Style.Font.Bold = true;
                recSheet.Cells[rrow, 1].Value = kvpair.Key;
                recSheet.Cells[rrow, 2].Style.Font.Bold = false;
                recSheet.Cells[rrow, 2].Value = kvpair.Value;
            }

            recSheet.Column(1).Width = 60;
            recSheet.Column(2).Width = 25;
            recSheet.Column(2).Style.WrapText = true;
            recSheet.Column(2).Style.JustifyLastLine = true;
            recSheet.Column(2).Style.ShrinkToFit = true;
            recSheet.Cells[1, 1, rrow, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            recSheet.Cells[1, 1, rrow, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Dashed;
            recSheet.Cells[1, 1, rrow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        }

        await package.SaveAsync();
        ms.Position = 0;
        return ms;
    }

    private List<Dictionary<string, string>> ExtractDictionaryToRender(IEnumerable<RefRequest> dataToRenderXlsx)
    {
        var records = new List<Dictionary<string, string>>();
        var users = _repo.GetAllUsers()
            .ToDictionary(z => z.UserId, z => z);

        foreach (var refRequest in dataToRenderXlsx)
        {
            var record = new Dictionary<string, string>();
            var startedTs = refRequest.Answers.Select(z => z.Timestamp).DefaultIfEmpty(refRequest.TimeStamp).Min();
            startedTs = startedTs > refRequest.TimeStamp ? refRequest.TimeStamp : startedTs;
            record.TryAdd("Дата заполнения",
                refRequest.TimeStamp.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));
            record.TryAdd("Дата начала заполнения",
                startedTs.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));

            record.TryAdd("Продолжтельность заполнения",
                (refRequest.TimeStamp - startedTs).ToString());

            if (users.TryGetValue(refRequest.UserId, out var user))
            {
                record.TryAdd("Telegram", $"@{user.Username}");
                record.TryAdd("Telegram ID", $"{user.UserId}");
            }

            foreach (var answ in refRequest.Answers)
                if (!record.TryAdd(answ.Question, answ.Answer))
                {
                    record[answ.Question] = $"{record[answ.Question]} {answ.Answer}" ;
                }

            records.Add(record);
        }

        return records;
    }

    private void CheckDuplicates(string[] headings, int row, ExcelWorksheet sheet)
    {
        int col;
        var colorList = Enum.GetValues(typeof(KnownColor))
            .Cast<KnownColor>()
            .ToList();
        var rnd = new Random(Environment.TickCount);

        col = 1;
        foreach (var heading in headings)
        {
            ++col;

            var quest = _questionnaire.Entries.FirstOrDefault(z => z.Text == heading);
            if (quest is not { DuplicateCheck: true })
                continue;
            var vals = new List<string>();
            for (var r = 1; r <= row; r++)
            {
                vals.Add(sheet.Cells[r, col].Text);
            }

            var duplicates = vals.GroupBy(z => z)
                .Where(z => z.Count() > 1);
            foreach (var duplicate in duplicates)
            {
                var color = Color.FromKnownColor(colorList[rnd.Next(0, colorList.Count)]);
                for (var r = 1; r <= row; r++)
                    if (sheet.Cells[r, col].Text == duplicate.Key)
                    {
                        sheet.Cells[r, col].Style.Fill.PatternType = ExcelFillStyle.LightTrellis;
                        sheet.Cells[r, col].Style.Fill.BackgroundColor.SetColor(color);
                    }
            }
        }
    }
}