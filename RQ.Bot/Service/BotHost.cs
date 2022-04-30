using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace RQ.Bot.Service;

/// <inheritdoc />
internal class BotHost : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly IUpdateHandler _botHandler;
    private readonly ILogger<BotHost> _logger;

    public BotHost(TelegramBotClient botClient, IUpdateHandler botHandler, ILogger<BotHost> logger)
    {
        _botClient  = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _botHandler = botHandler ?? throw new ArgumentNullException(nameof(botHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation("Bot {MeUsername} started", me.Username);

        _botClient.StartReceiving(
            _botHandler,
            receiverOptions: new ReceiverOptions { },
            stoppingToken
        );

        _logger.LogInformation("Start listening for {MeUsername}", me.Username);
    }
}