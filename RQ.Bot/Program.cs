using Prometheus;
using RQ.Bot.Extensions;

var builder = WebApplication
    .CreateBuilder(args)
    .Configure(args)
    .UseLiteDbDatabase()
    .UseTelegramBot()
    .UseQuestionnaire()
    .UseNextcloud();

var app = builder.Build();

app.UseSwagger()
    .UseSwaggerUI()
    .UseMetricServer();

app.MapControllers();

app.Run();