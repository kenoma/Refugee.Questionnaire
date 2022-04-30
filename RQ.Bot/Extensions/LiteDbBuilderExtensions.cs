using System;
using Bot.Repo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RQ.Bot.Extensions;

internal static class LiteDbBuilderExtensions
{
    public static WebApplicationBuilder UseLiteDbDatabase(this WebApplicationBuilder builder)
    {
        builder.Host.ConfigureServices((context, services) =>
        {
            var dbPath = context.Configuration["dbPath"];

            if (string.IsNullOrWhiteSpace(dbPath))
                throw new InvalidProgramException("Specify --dbPath argument");

            services.AddTransient<IRepository>(_ => new LiteDbRepo(dbPath));

        });
        return builder;
    }
}
