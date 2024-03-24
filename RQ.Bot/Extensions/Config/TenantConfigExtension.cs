namespace RQ.Bot.Extensions.Config;

public static class TenantConfigExtension
{
    public static IConfigurationBuilder AddTenantConfiguration(
        this IConfigurationBuilder builder, string apiBaseUrl, string clientId, string clientSecret, string configId)
    {
        return builder.Add(new TenantConfigurationSource(apiBaseUrl, clientId, clientSecret, configId));
    }
}