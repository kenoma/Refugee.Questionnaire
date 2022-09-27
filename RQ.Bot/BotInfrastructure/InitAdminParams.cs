namespace RQ.Bot.BotInfrastructure;

public class InitAdminParams
{
    /// <summary>
    ///     Айди пользователя в tg
    /// </summary>
    public long[] UsersUserIds { get; init; } = Array.Empty<long>();
}