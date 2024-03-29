namespace RQ.Bot.Integrations;

public class CrmRefRequestDTO
{
    /// <summary>
    ///     Айди заявки внутри бота
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    ///     Айди пользователя ТГ, оставившего заявку
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    ///     Айди чата с ботом (нужно, для отправки сообщений от имени бота)
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    ///     Дата создания заявки
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.MinValue;

    /// <summary>
    ///     Ответы на вопросы
    /// </summary>
    public CRMRefRequestEntryDTO[] Answers { get; set; } = Array.Empty<CRMRefRequestEntryDTO>();

    /// <summary>
    ///     Признак завершенности заявки (бывают отмененные)
    /// </summary>
    public bool IsCompleted { get; set; }
}

public abstract class CRMRefRequestEntryDTO
{
    /// <summary>
    ///     Текст вопроса, на который дан ответ
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    ///     Ответ
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    ///     Дата ответа
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.MinValue;
}