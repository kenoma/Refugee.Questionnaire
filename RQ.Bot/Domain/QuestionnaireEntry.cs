namespace RQ.DTO;

public class QuestionnaireEntry
{
    public string Text { get; set; } = string.Empty;

    public string ValidationRegex { get; set; } = string.Empty;

    public byte DuplicateCheck { get; set; } = 0;
    public string Category { get; set; } = string.Empty;

    public int Group { get; set; } = 0;

    public byte IsGroupSwitch { get; set; } = 0;
}