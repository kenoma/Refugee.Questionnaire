using Prometheus;
using RQ.Bot.Service;

namespace RQ.Bot.Extensions;

public static class PrometheusExtension
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder UsePrometheus(this WebApplicationBuilder builder)
    {
        builder.Host.ConfigureServices((context, services) =>
        {
            var prometheusPort = context.Configuration["prometheusPort"];

            if (!int.TryParse(prometheusPort, out var port))
                throw new InvalidProgramException("Specify --prometheusPort argument");

            services.AddSingleton<IMetricServer>(_ => new MetricServer(port));

            services.AddHostedService<PrometheusHost>();
        });
        return builder;
    }
}
