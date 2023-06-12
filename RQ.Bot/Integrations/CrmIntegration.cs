using System.Text;
using Newtonsoft.Json;
using RQ.Bot.Extensions.Config;
using RQ.DTO;

namespace RQ.Bot.Integrations;

public class CrmIntegration : IBotIntegration
{
    private readonly string _apiBaseUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _configId;
    private readonly ILogger<CrmIntegration> _logger;

    public CrmIntegration(string apiBaseUrl, string clientId, string clientSecret, string configId,
        ILogger<CrmIntegration> logger)
    {
        _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _configId = configId ?? throw new ArgumentNullException(nameof(configId));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PushRequestToIntegrationAsync(RefRequest refRequest)
    {
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        using var client = new HttpClient(handler);
        client.BaseAddress = new Uri(_apiBaseUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/help-refugees/bot-configuration/get-config/{_configId}");
        var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
            .GetBytes(_clientId + ":" + _clientSecret));
        request.Headers.Add("Authorization", $"Basic {encoded}");

        var message = new CrmRefRequestDto
        {
            Id = refRequest.Id,
            ChatId = refRequest.ChatId,
            UserId = refRequest.UserId,
            TimeStamp = refRequest.TimeStamp,
            IsCompleted = refRequest.IsCompleted,
            Answers = refRequest.Answers.Select(z => new CrmRefRequestEntryDto
            {
                Question = z.Question,
                Answer = z.Answer,
                Timestamp = z.Timestamp
            }).ToArray()
        };

        request.Content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var resp = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Responce {Callback}", resp);
    }
}