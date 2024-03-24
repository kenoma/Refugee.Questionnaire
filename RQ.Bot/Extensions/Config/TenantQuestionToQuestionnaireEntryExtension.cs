using RQ.Bot.Domain;

namespace RQ.Bot.Extensions.Config;

public static class TenantQuestionToQuestionnaireEntryExtension
{
    public static QuestionnaireEntry ToQuestionnaireEntry(this Question question)
    {
        return new QuestionnaireEntry
        {
            AutopassMode = question.AutopassMode,
            Text = question.Text.Trim(),
            Attachment = question.Attachment,
            Category = question.Category,
            Group = question.Group,
            DuplicateCheck = question.DuplicateCheck,
            PossibleResponses = question.AnswerVariants,
            ValidationRegex = question.ValidationRegex,
            IsGroupSwitch = question.IsGroupSwitch
        };
    }
}