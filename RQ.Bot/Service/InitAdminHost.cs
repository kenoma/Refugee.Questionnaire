using Bot.Repo;
using RQ.Bot.BotInfrastructure;
using RQ.DTO;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace RQ.Bot.Service;

/// <inheritdoc />
internal class InitAdminHost : BackgroundService
{
    private readonly IRepository _repository;
    private readonly InitAdminParams _adminParams;
    private readonly ILogger<InitAdminHost> _logger;

    public InitAdminHost(IRepository repository, InitAdminParams adminParams, ILogger<InitAdminHost> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _adminParams = adminParams ?? throw new ArgumentNullException(nameof(adminParams));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_adminParams.UserId.HasValue)
        {
            _logger.LogInformation("No --adminID params passed to app");
            return Task.CompletedTask;
        }

        if (_repository.GetAdminUsers().Any())
        {
            _logger.LogInformation("There are already admins at database");
            return Task.CompletedTask;
        }

        _repository.UpsertUser(new UserData
        {
            UserId = _adminParams.UserId.Value,
            IsAdmin = true
        });

        _logger.LogInformation("Initial admin added {AdminId}", _adminParams.UserId);
        return Task.CompletedTask;
    }
}