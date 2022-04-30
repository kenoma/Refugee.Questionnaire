using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RQ.Bot.Extensions;

internal static class MongoDbBuilderExtensions
{
    public static IHostBuilder UseMongoDatabaseAndService(this IHostBuilder builder)
    {
        return builder.ConfigureServices(collection =>
        {
            // collection
            //     .AddSingleton(provider =>
            //     {
            //
            //         throw new InvalidOperationException("Fatal - mongodb service address is not resolved");
            //
            //     });
        });
    }
}
