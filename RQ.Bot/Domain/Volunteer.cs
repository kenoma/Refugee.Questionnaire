namespace RQ.DTO;

public class Volunteer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Идентификатор пользователя в сети ТГ
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    ///     Уникальный токен для доступа к кешированным данным бота
    /// </summary>
    public string Token { get; set; } = Guid.NewGuid().ToString();
}