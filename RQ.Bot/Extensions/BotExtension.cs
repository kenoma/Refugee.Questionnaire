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
        builder.Services.AddSingleton(_ =>
            {
                var botToken = builder.Configuration["botToken"];

                if (string.IsNullOrWhiteSpace(botToken))
                    throw new InvalidProgramException("Specify --botToken argument");

                return new TelegramBotClient(botToken);
            })
            .AddTransient<IUpdateHandler, BotLogic>()
            .AddTransient<EntryAdmin>()
            .AddTransient<EntryQuestionnaire>()
            .AddTransient<EntryDownloadCsv>()
            .AddSingleton(_ =>
            {
                var rawUserId = builder.Configuration["adminID"];

                return long.TryParse(rawUserId, out var userId)
                    ? new InitAdminParams { UserId = userId }
                    : new InitAdminParams();
            })
            .AddSingleton(_ =>
            {
                var sorting = builder.Configuration["sorting"];

                return new ReportGenerationParams { IsDescendingSorting = sorting == "desc" };
            });

        builder.Host.ConfigureServices((_, services) =>
        {
            services.AddHostedService<BotHost>();
            services.AddHostedService<InitAdminHost>();
        });
        return builder;
    }
}