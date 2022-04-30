namespace RQ.DTO;

public class Questionnaire
{
    public QuestionnaireEntry[] Entries { get; set; } = Array.Empty<QuestionnaireEntry>();
}