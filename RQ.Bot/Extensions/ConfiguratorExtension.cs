using System.Reflection;
using Microsoft.Extensions.Logging.Console;
using RQ.Bot.Extensions.Config;

namespace RQ.Bot.Extensions;

/// <summary>
/// 
/// </summary>
public static class ConfiguratorExtension
{
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder, params string[] args)
    {
        builder.Host.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddCommandLine(args);
            configurationBuilder.AddEnvironmentVariables();
            var intermedCofig = configurationBuilder.Build();
            var apiBaseUrl = intermedCofig["apiBaseUrl"];
            var clientId = intermedCofig["clientId"];
            var clientSecret = intermedCofig["clientSecret"];
            var configId = intermedCofig["configId"];
            
            configurationBuilder.AddTenantConfiguration(apiBaseUrl, clientId, clientSecret, configId);
        });

        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.TimestampFormat = "dd:MM:yyyy hh:mm:ss ";
        });

        builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        return builder;
    }
}