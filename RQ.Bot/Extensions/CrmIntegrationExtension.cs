using Bot.Repo;
using RQ.Bot.Integrations;

namespace RQ.Bot.Extensions;

internal static class CrmIntegrationExtension
{
    public static WebApplicationBuilder UseCrmIntegration(this WebApplicationBuilder builder)
    {
        builder.Host.ConfigureServices((context, services) =>
        {
            var apiBaseUrl = context.Configuration["apiBaseUrl"];
            var clientId = context.Configuration["clientId"];
            var clientSecret = context.Configuration["clientSecret"];
            var configId = context.Configuration["configId"];

            services.AddTransient<IBotIntegration>(s => new CrmIntegration(apiBaseUrl, clientId, clientSecret, configId,
                s.GetRequiredService<ILogger<CrmIntegration>>()));
        });
        return builder;
    }
}