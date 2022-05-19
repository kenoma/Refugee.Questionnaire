using System.Reflection;

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
            
        // builder.Host.UseSerilog((ctx, logCfg) =>
        // {
        //     //var logSection = ctx.Configuration["loggerSection"] ?? LoggerConvention.DefaultSection;
        //
        //     logCfg //.ReadFrom.Configuration(ctx.Configuration)
        //         .WriteTo.Console(theme: AnsiConsoleTheme.Code).MinimumLevel.Verbose();
        //
        // });
        
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
