namespace RQ.DTO;

public class RefRequestEntry
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.Now;
}