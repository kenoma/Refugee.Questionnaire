using RQ.Bot.Extensions;

var builder = WebApplication
    .CreateBuilder(args)
    .Configure(args)
    .UsePrometheus()
    .UseLiteDBDatabase()
    .UseTelegramBot();

var app = builder.Build();

app.UseSwagger()
    .UseSwaggerUI();

app.MapControllers();

app.Run();