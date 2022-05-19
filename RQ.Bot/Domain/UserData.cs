// ReSharper disable UnusedAutoPropertyAccessor.Global
using LiteDB;

namespace RQ.DTO;

public class UserData
{
    /// <summary>
    ///     Идентификатор пользователя в сети ТГ
    /// </summary>
    [BsonId]
    public long UserId { get; set; }

    /// <summary>
    ///     Идентфикатор чата, в котором бот общается с пользователем
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    ///     Уникальный токен для доступа к кешированным данным бота
    /// </summary>
    public string Token { get; set; } = Guid.NewGuid().ToString();

    public bool IsAdmin { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public long PromotedByUser { get; set; }

    /// <summary>
    ///     Дата\время создания пользователя
    /// </summary>
    public DateTime Created { get; set; } = DateTime.Now;
}