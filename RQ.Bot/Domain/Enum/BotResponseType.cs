namespace RQ.Bot.Domain.Enum;

public enum BotResponseType
{
    None = 0,
    CurrentXlsx,
    AllXlsx,
    CurrentCsv,
    AllCsv,
    Archive,
    SwitchNotifications,
    ReplyToUser,
    MessageToAdmins,
    QMove,
    QRem,
    QFinish,
    QReturn,
    FillRequest
}