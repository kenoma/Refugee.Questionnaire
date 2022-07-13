using System.Reflection;
using Microsoft.Extensions.Logging.Console;

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
            
        });
        
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.TimestampFormat = "dd:MM:yyyy hh:mm:ss ";
        });
        
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
