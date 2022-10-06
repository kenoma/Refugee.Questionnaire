using RQ.Bot.Domain.Enum;

namespace RQ.Bot.Domain;

public class QuestionnaireEntry
{
    /// <summary>
    /// Текст вопроса
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Регулярное выражение для проверки ответа
    /// </summary>
    public string ValidationRegex { get; set; } = string.Empty;

    /// <summary>
    /// Проверка на дубль похожих ответов при генерации ответов
    /// </summary>
    public byte DuplicateCheck { get; set; } = 0;

    /// <summary>
    /// Категория вопроса (для генерации меню с кнопками)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Группа вопроса (для пропуска пачки вопросов)
    /// </summary>
    public int Group { get; set; } = 0;

    /// <summary>
    /// Является ли вопрос главным для группы?  (отрицательный ответ пропускает группу)
    /// </summary>
    public byte IsGroupSwitch { get; set; } = 0;

    /// <summary>
    /// Сообщения, которые выводятся автоматически, без подтверждения пользователем.
    /// <remarks>
    ///     0 - не является пропускаемым сообщением.
    ///     1 - обычное сообщение, которое проскакивает в тексте.
    ///     2 - Сообщения, которые всегда идут в начале анкеты.
    ///     3 - Сообщения, которые всегда идут в конце.
    /// </remarks>
    /// </summary>
    public AutopassMode AutopassMode { get; set; }

    /// <summary>
    /// Адрес картинки или видео для вложения к сообщению.
    /// Картинки только png или jpg.
    /// Видео только из файла.
    /// </summary>
    public string Attachment { get; set; }

    /// <summary>
    /// Варианты ответа.
    /// </summary>
    public IEnumerable<string> PossibleResponses { get; set; }
}