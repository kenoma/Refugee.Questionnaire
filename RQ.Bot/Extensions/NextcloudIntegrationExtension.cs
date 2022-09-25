using Bot.Repo;
using RQ.Bot.Integrations;

namespace RQ.Bot.Extensions;

public static class NextcloudIntegrationExtension
{
    public static WebApplicationBuilder UseNextcloud(this WebApplicationBuilder builder)
    {
        var nextCloudLogin = builder.Configuration["nextcloudLogin"];
        var nextCloudPassword = builder.Configuration["nextcloudPass"];
        var nextCloudUrl = builder.Configuration["nextcloudUrl"];
        var nextCloudDeckIndex = builder.Configuration["nextcloudDeckIndex"];

        if (string.IsNullOrEmpty(nextCloudUrl))
        {
            return builder;
        }

        builder.Services.AddSingleton<IBotIntegration>(z => new NextcloudDeck(
            z.GetRequiredService<IHttpClientFactory>(),
            z.GetRequiredService<ILogger<NextcloudDeck>>(),
            z.GetRequiredService<IRepository>(),
            nextCloudLogin,
            nextCloudPassword,
            nextCloudUrl,
            int.TryParse(nextCloudDeckIndex, out var dindex) ? dindex : 0));

        return builder;
    }
}