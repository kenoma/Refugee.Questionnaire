using System;
using System.Diagnostics;
using CvLab.TelegramBot.Service;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Prometheus;

namespace RQ.Bot.Extensions;

public static class PrometheusExtension
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IHostBuilder UsePrometheus(this IHostBuilder builder) => builder.ConfigureServices((context, services)  =>
    {
        var prometheusPort = context.Configuration["prometheusPort"];

        if (!int.TryParse(prometheusPort, out var port))
            throw new InvalidProgramException("Specify --prometheusPort argument");

        services.AddSingleton<IMetricServer>(_ => new MetricServer(port));

        services.AddHostedService<PrometheusHost>();
    });
}
