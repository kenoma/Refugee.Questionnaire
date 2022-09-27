namespace RQ.DTO.Enum;

public enum BotResponceType
{
    None = 0,

    get_current_xlsx,
    get_all_xlsx,
    get_current_csv,
    get_all_csv,
    archive,
    list_admins,
    switch_notifications,
    remove_user,
    reply_to_user,
    message_to_admins,
    add_permitions
}