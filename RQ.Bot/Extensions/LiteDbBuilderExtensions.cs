using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RQ.Bot.Extensions;

internal static class LiteDbBuilderExtensions
{
    public static WebApplicationBuilder UseLiteDBDatabase(this WebApplicationBuilder builder)
    {
        // return builder.ConfigureServices(collection =>
        // {
        //     // collection
        //     //     .AddSingleton(provider =>
        //     //     {
        //     //
        //     //         throw new InvalidOperationException("Fatal - mongodb service address is not resolved");
        //     //
        //     //     });
        // });
        return builder;
    }
}
