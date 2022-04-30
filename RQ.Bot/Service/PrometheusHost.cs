using Prometheus;
using Serilog;

namespace CvLab.TelegramBot.Service;

internal class PrometheusHost : IHostedService
{
    private readonly IMetricServer _metricServer;
    private readonly Serilog.ILogger _logger = Log.ForContext<PrometheusHost>();

    /// <summary>
    ///     Creates application host from container
    /// </summary>
    /// <param name="metricServer"></param>
    /// <param name="logger"></param>
    public PrometheusHost(IMetricServer metricServer)
    {
        _metricServer = metricServer ?? throw new ArgumentNullException(nameof(metricServer));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metricServer.Start();
        _logger.Information("Prometheus host started");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _metricServer
            .StopAsync()
            .ConfigureAwait(false);

        _logger.Information("Prometheus host stopped");
    }
}
