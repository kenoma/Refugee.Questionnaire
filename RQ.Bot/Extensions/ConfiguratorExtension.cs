using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace RQ.Bot.Extensions;

/// <summary>
/// 
/// </summary>
public static class ConfiguratorExtension
{
    public static IHostBuilder Configure(this IHostBuilder builder, params string[] args) => builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddCommandLine(args);
            configurationBuilder.AddEnvironmentVariables();
            var config = configurationBuilder.Build();

            var consulAddress = config["consulAddress"];

            if (string.IsNullOrEmpty(consulAddress))
                consulAddress = "http://localhost:8500";

            var consulToken = config["consulToken"];

            var configName = config["configName"];

            if (string.IsNullOrEmpty(configName))
                configName = "default";
            
        })
        .UseSerilog((ctx, logCfg) =>
        {
            //var logSection = ctx.Configuration["loggerSection"] ?? LoggerConvention.DefaultSection;

            logCfg//.ReadFrom.Configuration(ctx.Configuration)
                .WriteTo.Console(theme: AnsiConsoleTheme.Code).MinimumLevel.Verbose();

        });
}
