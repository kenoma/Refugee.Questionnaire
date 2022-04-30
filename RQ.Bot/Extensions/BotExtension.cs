using RQ.Bot.BotInfrastructure;
using RQ.Bot.BotInfrastructure.Entry;
using RQ.Bot.Service;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace RQ.Bot.Extensions;

public static class BotExtension
{
    public static WebApplicationBuilder UseTelegramBot(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(z =>
            {
                var botToken = builder.Configuration["botToken"];

                if (string.IsNullOrWhiteSpace(botToken))
                    throw new InvalidProgramException("Specify --botToken argument");

                return new TelegramBotClient(botToken);
            })
            .AddTransient<IUpdateHandler, BotLogic>()
            .AddTransient<EntryConfigureChat>();

        builder.Host.ConfigureServices((_, services) =>
        {
            services.AddHostedService<BotHost>();
        });
        return builder;
    }
}