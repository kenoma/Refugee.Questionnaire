using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Serilog;

using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;

namespace CvLab.TelegramBot.Service
{
    /// <inheritdoc />
    internal class BotHost : BackgroundService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IUpdateHandler _botHandler;

        public BotHost(TelegramBotClient botClient, IUpdateHandler botHandler)
        {
            _botClient  = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _botHandler = botHandler ?? throw new ArgumentNullException(nameof(botHandler));
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var me = await _botClient.GetMeAsync(stoppingToken);
            Log.Information("Bot {MeUsername} started", me.Username);

            _botClient.StartReceiving(
                _botHandler,
                receiverOptions: new ReceiverOptions { },
                stoppingToken
            );

            Log.Information("Start listening for {MeUsername}", me.Username);
        }
    }
}
