using RQ.Bot.Extensions;

var builder = WebApplication
    .CreateBuilder(args)
    .Configure(args)
    .UsePrometheus()
    .UseLiteDbDatabase()
    .UseTelegramBot();

var app = builder.Build();

app.UseSwagger()
    .UseSwaggerUI();

app.MapControllers();

app.Run();