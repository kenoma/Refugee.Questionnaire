using Bot.Repo;
using RQ.Bot.BotInfrastructure;
using RQ.DTO;

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
        if (!_adminParams.UsersUserIds.Any())
        {
            _logger.LogInformation("No --adminID params passed to app");
            return Task.CompletedTask;
        }

        var allUsers = _repository.GetAdminUsers();
        foreach (var userData in allUsers)
        {
            userData.IsAdministrator = false;
            _repository.UpsertUser(userData);
        }

        foreach (var userId in _adminParams.UsersUserIds)
        {
            if (_repository.TryGetUserById(userId, out var data))
            {
                data.IsAdministrator = true;
                _repository.UpsertUser(data);
            }
            else
            {
                _repository.UpsertUser(new UserData
                {
                    IsAdministrator = true,
                    UserId = userId
                });
            }
        }

        _logger.LogInformation("Initial admin added {AdminId}", _adminParams.UsersUserIds);
        return Task.CompletedTask;
    }
}