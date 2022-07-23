namespace RQ.DTO;

public class RefRequest
{
    private DateTime _timeStamp;
    public Guid Id { get; set; } = Guid.NewGuid();

    public long UserId { get; set; }
    
    public long ChatId { get; set; }

    public DateTime TimeStamp
    {
        get
        {
            return Answers
                .Where(z => (z.Timestamp - DateTime.Now).Duration() > TimeSpan.FromMinutes(1))
                .Select(z => z.Timestamp)
                .DefaultIfEmpty(_timeStamp)
                .Max();
        }
        init => _timeStamp = value;
    }

    public RefRequestEntry[] Answers { get; set; } = Array.Empty<RefRequestEntry>();
    public bool IsCompleted { get; set; }
    public string CurrentCategory { get; set; }

    public override string ToString() =>
        $"{TimeStamp}: {(IsCompleted ? "заполнен" : "в процессе")} {Answers.FirstOrDefault()?.Answer} ";
}