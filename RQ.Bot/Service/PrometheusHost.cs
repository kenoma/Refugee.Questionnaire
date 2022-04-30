using Prometheus;

namespace RQ.Bot.Service;

internal class PrometheusHost : IHostedService
{
    private readonly IMetricServer _metricServer;
    private readonly ILogger<PrometheusHost> _logger;

    /// <summary>
    ///     Creates application host from container
    /// </summary>
    /// <param name="metricServer"></param>
    /// <param name="logger"></param>
    public PrometheusHost(IMetricServer metricServer, ILogger<PrometheusHost> logger)
    {
        _metricServer = metricServer ?? throw new ArgumentNullException(nameof(metricServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metricServer.Start();
        _logger.LogInformation("Prometheus host started");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _metricServer
            .StopAsync()
            .ConfigureAwait(false);

        _logger.LogInformation("Prometheus host stopped");
    }
}