namespace RQ.Bot.Domain;

public class Questionnaire
{
    public IList<QuestionnaireEntry> Entries { get; set; } // = Array.Empty<QuestionnaireEntry>();
    public IList<QuestionnaireEntry> Headliners { get; set; } // = Array.Empty<QuestionnaireEntry>();
    public IList<QuestionnaireEntry> Finishers { get; set; } // = Array.Empty<QuestionnaireEntry>();
}