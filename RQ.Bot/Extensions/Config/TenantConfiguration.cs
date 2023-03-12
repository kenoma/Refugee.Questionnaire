using Newtonsoft.Json;
using RQ.Bot.Domain.Enum;

namespace RQ.Bot.Extensions.Config;

public class Question
{
    [JsonProperty("surveyDefId")] public string SurveyDefId { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("orderPosition")] public int OrderPosition { get; set; }

    [JsonProperty("text")] public string Text { get; set; }

    [JsonProperty("validationRegex")] public string ValidationRegex { get; set; }

    [JsonProperty("duplicateCheck")] public bool DuplicateCheck { get; set; }

    [JsonProperty("category")] public string Category { get; set; }

    [JsonProperty("group")] public int Group { get; set; }

    [JsonProperty("isGroupSwitch")] public bool IsGroupSwitch { get; set; }

    [JsonProperty("autopassMode")] public AutopassMode AutopassMode { get; set; }

    [JsonProperty("attachmentFileDataId")] public string AttachmentFileDataId { get; set; }

    [JsonProperty("attachment")] public string Attachment { get; set; }

    [JsonProperty("answerVariants")] public string[] AnswerVariants { get; set; } = Array.Empty<string>();

    [JsonProperty("isDeleted")] public bool IsDeleted { get; set; }

    [JsonProperty("id")] public string Id { get; set; }
}

public class TenantConfiguration
{
    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("tenantId")] public string TenantId { get; set; }

    [JsonProperty("surveyDefId")] public string SurveyDefId { get; set; }

    [JsonProperty("surveyDef")] public SurveyDef SurveyDef { get; set; }

    [JsonProperty("prometheusPort")] public int PrometheusPort { get; set; }

    [JsonProperty("urls")] public string[] Urls { get; set; } = Array.Empty<string>();

    [JsonProperty("dbPath")] public string DbPath { get; set; }

    [JsonProperty("botToken")] public string BotToken { get; set; }

    [JsonProperty("admins")] public long[] Admins { get; set; } = Array.Empty<long>();

    [JsonProperty("id")] public string Id { get; set; }
}

public class SurveyDef
{
    [JsonProperty("tenantId")] public string TenantId { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("isActive")] public bool IsActive { get; set; }

    [JsonProperty("isDeleted")] public bool IsDeleted { get; set; }

    [JsonProperty("questions")] public Question[] Questions { get; set; } = Array.Empty<Question>();

    [JsonProperty("id")] public string Id { get; set; }
}