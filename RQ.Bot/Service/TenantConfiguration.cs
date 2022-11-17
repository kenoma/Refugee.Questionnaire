using RQ.Bot.Domain;

namespace RQ.Bot.Service;

public class TenantConfiguration
{
    /// <summary>
    ///     Порт диагностических данных о работе бота
    /// </summary>
    public int PrometheusPort { get; set; }

    /// <summary>
    ///     Список урлов, на которые будет забинден API бота
    /// </summary>
    public string[] Urls { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     Путь к внутреннему хранилищу данных бота
    /// </summary>
    public string DbPath { get; set; }

    /// <summary>
    ///     Токен бота в ТГ
    /// </summary>
    public string BotToken { get; set; }

    /// <summary>
    ///     Опросник, с которым работает указанный бот
    /// </summary>
    public Questionnaire Questionnaire { get; set; }

    /// <summary>
    ///     Список идентификаторов администраторов для бота
    /// </summary>
    public long[] Admins { get; set; } = Array.Empty<long>();
}