using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace RQ.Bot.Extensions.Config;

public class TenantConfigurationProvider : ConfigurationProvider
{
    private readonly string _apiBaseUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _configId;

    public TenantConfigurationProvider(string apiBaseUrl, string clientId, string clientSecret, string configId)
    {
        _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _configId = configId ?? throw new ArgumentNullException(nameof(configId));
    }

    public override void Load()
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
        var response = client.Send(request);
        response.EnsureSuccessStatusCode();

        var configJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var configDto = JsonConvert.DeserializeObject<TenantConfiguration>(configJson);

        FillConfigParameters(configDto);
    }

    private void FillConfigParameters(TenantConfiguration configDto)
    {
        Set("tenantId", configDto.TenantId);

        if (!string.IsNullOrWhiteSpace(configDto.BotToken))
        {
            Set("botToken", configDto.BotToken);
        }

        if (!string.IsNullOrWhiteSpace(configDto.DbPath))
        {
            Set("dbPath", configDto.DbPath);
        }

        Set("adminID", string.Join(",", configDto.Admins));
        Set("questionnaireRaw", JsonConvert.SerializeObject(configDto.SurveyDef.Questions));
    }
}