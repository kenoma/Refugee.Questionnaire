using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Bot.Repo;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RQ.DTO;

namespace RQ.Bot.Integrations;

public class NextcloudDeck : IBotIntegration
{
    private const string BotStackName = "Входящие от бота";
    private readonly string _nextCloudLogin;
    private readonly string _nextCloudPassword;
    private readonly string _nextCloudUrl;
    private readonly int _nextcloudBoardId;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<NextcloudDeck> _logger;
    private readonly IRepository _repo;

    public NextcloudDeck(IHttpClientFactory clientFactory, ILogger<NextcloudDeck> logger, IRepository repo,
        string nextCloudLogin, string nextCloudPassword,
        string nextCloudUrl, int nextcloudBoardId)
    {
        _nextCloudLogin = nextCloudLogin ?? throw new ArgumentNullException(nameof(nextCloudLogin));
        _nextCloudPassword = nextCloudPassword ?? throw new ArgumentNullException(nameof(nextCloudPassword));
        _nextCloudUrl = nextCloudUrl ?? throw new ArgumentNullException(nameof(nextCloudUrl));
        _nextcloudBoardId = nextcloudBoardId;
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public async Task PushRequestToIntegrationAsync(RefRequest refRequest)
    {
        var stacks = await GetStacksAsync();
        var targetStack = stacks.FirstOrDefault(z => z.Title == BotStackName) ?? await CreateStack();

        var card = await CreateCard(targetStack, refRequest);

        await UploadAttachment(targetStack, card, refRequest);

        _logger.LogDebug("New nexcloud card:{@Card}", card);
    }

    private async Task UploadAttachment(NextCloudStack targetStack, NextcloudCard card, RefRequest refRequest)
    {
        var httpClient = _clientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_nextCloudUrl}/index.php/apps/deck/api/v1.0/boards/{_nextcloudBoardId}/stacks/{targetStack.Id}/cards/{card.Id}/attachments");

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("OCS-APIRequest", "true");

        var base64Authorization =
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_nextCloudLogin}:{_nextCloudPassword}"));
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

        var blank = await GetXlsxFileAsync(refRequest);
        
        var requestContent = new MultipartFormDataContent(); 
        var imageContent = new ByteArrayContent(blank);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        requestContent.Add(imageContent, "file", $"request_{refRequest.UserId}.xlsx");
        requestContent.Add(new StringContent("deck_file"), "type");
        //
        request.Content = requestContent;

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var respData = await response.Content.ReadAsStringAsync();

        _logger.LogWarning("Failed to create stack for bot incoming {Error}", respData);
        throw new InvalidOperationException("Failed to create stack for bot incoming");
    }

    private async Task<byte[]> GetXlsxFileAsync(RefRequest refRequest)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var ms = new MemoryStream();
        using var package = new ExcelPackage(ms);
        var sheet = package.Workbook.Worksheets.Add($"Заявка от {refRequest.UserId}");

        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 1].Value = "Вопрос";
        sheet.Cells[1, 1].Style.Border.BorderAround(ExcelBorderStyle.Thick);
        sheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        

        sheet.Cells[1, 2].Style.Font.Bold = true;
        sheet.Cells[1, 2].Value = "Ответ";
        sheet.Cells[1, 2].Style.Border.BorderAround(ExcelBorderStyle.Thick);
        sheet.Cells[1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        

        var row = 1;
        foreach (var item in refRequest.Answers)
        {
            row++;
            sheet.Cells[row, 1].Style.Font.Bold = false;
            sheet.Cells[row, 1].Value = item.Question;

            sheet.Cells[row, 2].Style.Font.Bold = true;
            sheet.Cells[row, 2].Value = item.Answer;
        }

        sheet.Cells[1, 1, row, 2].AutoFitColumns();

        await package.SaveAsync();
        ms.Position = 0;
        return ms.ToArray();
    }

    private async Task<IEnumerable<NextCloudStack>> GetStacksAsync()
    {
        var httpClient = _clientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_nextCloudUrl}/index.php/apps/deck/api/v1.0/boards/{_nextcloudBoardId}/stacks");

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("OCS-APIRequest", "true");

        var base64Authorization =
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_nextCloudLogin}:{_nextCloudPassword}"));
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var respData = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<NextCloudStack[]>(respData);
        }

        _logger.LogWarning("Failed to request nextcloud deck stasks");
        return Array.Empty<NextCloudStack>();
    }

    private async Task<NextCloudStack> CreateStack()
    {
        var httpClient = _clientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_nextCloudUrl}/index.php/apps/deck/api/v1.0/boards/{_nextcloudBoardId}/stacks");

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("OCS-APIRequest", "true");

        var base64Authorization =
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_nextCloudLogin}:{_nextCloudPassword}"));
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

        request.Content = new StringContent($"{{ \"title\" : \"{BotStackName}\", \"order\" : 0 }}");
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<NextCloudStack>();
        }

        _logger.LogWarning("Failed to create stack for bot incoming");
        throw new InvalidOperationException("Failed to create stack for bot incoming");
    }

    private async Task<NextcloudCard> CreateCard(NextCloudStack nextCloudStack, RefRequest refRequest)
    {
        var httpClient = _clientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_nextCloudUrl}/index.php/apps/deck/api/v1.0/boards/{_nextcloudBoardId}/stacks/{nextCloudStack.Id}/cards");

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("OCS-APIRequest", "true");

        var base64Authorization =
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_nextCloudLogin}:{_nextCloudPassword}"));
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

        var blank = GetCardFromRequest(refRequest);
        request.Content = JsonContent.Create(blank);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<NextcloudCard>();
        }

        var respData = await response.Content.ReadAsStringAsync();

        _logger.LogWarning("Failed to create stack for bot incoming {Error}", respData);
        throw new InvalidOperationException("Failed to create stack for bot incoming");
    }

    private NextcloudCard GetCardFromRequest(RefRequest refRequest)
    {
        var username = refRequest.UserId.ToString();
        if (_repo.TryGetUserById(refRequest.UserId, out var user))
        {
            username = $"@{user.Username} (tg id: {user.UserId})";
        }

        var mdescr = new StringBuilder()
            .AppendLine("|  |  |")
            .AppendLine("|-|-|");

        mdescr.AppendLine($"| Id пользователя в телеграмме |{JsonConvert.ToString(refRequest.UserId)}|");
        mdescr.AppendLine($"| Имя пользователя в телеграмме |{JsonConvert.ToString(user?.Username)}|");
        mdescr.AppendLine($"| Идентификатор анкеты |{JsonConvert.ToString(refRequest.Id)}|");
        mdescr.AppendLine($"| Количество ответов |{JsonConvert.ToString(refRequest.Answers.Length)}|");
        mdescr.AppendLine($"| Дата завершения заполнения |{JsonConvert.ToString(refRequest.TimeStamp)}|");
        mdescr.AppendLine($"| Признак заполнения анкеты |{JsonConvert.ToString(refRequest.IsCompleted)}|");
        mdescr.AppendLine($"| Признак прерванной анкеты |{JsonConvert.ToString(refRequest.IsInterrupted)}|");
        
        return new NextcloudCard
        {
            Title = $"{refRequest.TimeStamp:dd.MM.yy HH:mm} от {username}",
            Description = mdescr.ToString(),
            Order = Environment.TickCount
        };
    }

    private sealed class NextcloudCard
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "plain";

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("order")] public int Order { get; set; }

        [JsonPropertyName("duedate")] public DateTime? Duedate { get; set; }

        [JsonPropertyName("id")] public int Id { get; set; }
    }

    private sealed class NextCloudStack
    {
        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("id")] public int Id { get; set; }
    }
}