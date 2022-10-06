namespace RQ.Bot.Domain.Enum;

/// <summary>
/// Тип сообщения.
/// </summary>
public enum AutopassMode
{
    /// <summary>
    /// Не является пропускаемым сообщением.
    /// </summary>
    None = 0,

    /// <summary>
    /// Обычное сообщение, которое проскакивает в тексте.
    /// </summary>
    Simple = 1,

    /// <summary>
    /// Сообщения, которые всегда идут в начале анкеты.
    /// </summary>
    Headline = 2,

    /// <summary>
    /// Сообщения, которые всегда идут в конце.
    /// </summary>
    Finisher = 3
}
