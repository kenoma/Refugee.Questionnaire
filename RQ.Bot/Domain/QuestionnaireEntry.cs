namespace RQ.DTO;

public class QuestionnaireEntry
{
    /// <summary>
    ///     Текст вопроса
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    ///     Регулярное выражение для проверки ответа
    /// </summary>
    public string ValidationRegex { get; set; } = string.Empty;

    /// <summary>
    ///     Проверка на дубль похожих ответов при генерации ответов
    /// </summary>
    public byte DuplicateCheck { get; set; } = 0;
    
    /// <summary>
    ///     Категория вопроса (для генерации меню с кнопками)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     Группа вопроса (для пропуска пачки вопросов)
    /// </summary>
    public int Group { get; set; } = 0;

    /// <summary>
    ///     Является ли вопрос главным для группы?  (отрицательный ответ пропускает группу)
    /// </summary>
    public byte IsGroupSwitch { get; set; } = 0;

    /// <summary>
    ///     Является ли вопрос информационным, после которого мы переходим к следующему?
    /// </summary>
    public bool IsAutoPass { get; set; }
}