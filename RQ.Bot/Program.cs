using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Prometheus;
using RQ.Bot.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .UseRestAPI()
    .Build();
    //.Configure(args)
    //.UseMongoDatabaseAndService()
    //.UsePrometheus()

await builder
    .RunAsync()
    .ConfigureAwait(false);

// namespace CvLab.TelegramBot
// {
//     class Program
//     {
//         public static async Task Main(
//             
//         )
//         {
//             var builder = WebHost.CreateDefaultBuilder()
//                 .UseSerilog((ctx, logCfg) => logCfg
//                     .Enrich.WithProcessName()
//                     .Enrich.WithProcessId()
//                     .Enrich.WithExceptionDetails()
//                     .Enrich.WithMachineName()
//                     .Enrich.WithEnvironmentUserName()
//                     .MinimumLevel.Is(logLevel)
//                     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
//                     .MinimumLevel.Override("System", LogEventLevel.Warning)
//                     .WriteTo.Console())
//                 .ConfigureServices((hostContext, services) =>
//                 {
//                     services.AddSingleton(_ =>
//                     {
//                         var cfg = new ServiceStates(stateFile);
//
//                         return cfg;
//                     });
//
//                     services.AddSingleton(z =>
//                     {
//                         if (!useProxy)
//                             return new TelegramBotClient(botToken);
//
//                         var proxy = new WebProxy(proxyHost, proxyPort) {UseDefaultCredentials = true};
//
//                         return new TelegramBotClient(botToken, proxy);
//                     });
//
//                     services.AddSingleton<EntryIssues>();
//                     services.AddTransient<EntryProjectsAndMilestoneSelect>();
//                     services.AddTransient<EntryConfigureChat>();
//                     services.AddTransient<EntryIssueCreate>();
//                     services.AddTransient<ReportPublisher>();
//
//                     services.AddTransient<NotificationService>();
//
//                     services.AddTransient<GiltabClientProvider>();
//                     services.AddTransient<IUpdateHandler, BotLogic>();
//
//                     services.AddHostedService<BotHost>();
//                     services.AddHostedService<ReportingHost>();
//
//                     services.AddTransient<QuestionariesArchiveController>();
//
//                     services.AddControllers();
//                     services.AddSwaggerGen();
//                 })
//                 
//                 .Build();
//
//             Log.Information("WebHook receiving started at port {Port}", webHookServicePort);
//             await builder
//                 
//                 .ConfigureAwait(false);
//         }
//     }
// }