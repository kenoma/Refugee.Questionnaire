namespace RQ.Bot.Extensions.Config;

public class TenantConfigurationSource : IConfigurationSource
{
    private readonly string _apiBaseUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _configId;

    public TenantConfigurationSource(string apiBaseUrl, string clientId, string clientSecret, string configId)
    {
        _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _configId = configId ?? throw new ArgumentNullException(nameof(configId));
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new TenantConfigurationProvider(_apiBaseUrl, _clientId,_clientSecret, _configId);
}