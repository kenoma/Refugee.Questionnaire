namespace RQ.Bot.Domain;

public class Questionnaire
{
    public IList<QuestionnaireEntry> Entries { get; }
    public IList<QuestionnaireEntry> Headliners { get; }
    public IList<QuestionnaireEntry> Finishers { get; }

    public Questionnaire()
    {
        Entries = new List<QuestionnaireEntry>();
        Headliners = new List<QuestionnaireEntry>();
        Finishers = new List<QuestionnaireEntry>();
    }
}