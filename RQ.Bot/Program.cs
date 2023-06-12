using Prometheus;
using RQ.Bot.Extensions;

var builder = WebApplication
    .CreateBuilder(args)
    .Configure(args)
    .UseLiteDbDatabase()
    .UseTelegramBot()
    .UseQuestionnaire()
    .UseCrmIntegration();

var app = builder.Build();

app.UseSwagger()
    .UseSwaggerUI()
    .UseMetricServer();

app.MapControllers();

app.Run();