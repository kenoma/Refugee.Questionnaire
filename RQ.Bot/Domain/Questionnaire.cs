namespace RQ.DTO;

public class Questionnaire
{
    public QuestionnaireEntry[] Entries { get; set; } = Array.Empty<QuestionnaireEntry>();
    public QuestionnaireEntry[] Headliners { get; set; } = Array.Empty<QuestionnaireEntry>();
    public QuestionnaireEntry[] Finishers { get; set; } = Array.Empty<QuestionnaireEntry>();
}