using RQ.DTO;

namespace Bot.Repo;

public class LiteDbRepo : IRepository
{
    private readonly string _dbPath;

    public LiteDbRepo(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    public bool TryGetChatData(long chatId, out ChatConfiguration chatConfig)
    {
        throw new NotImplementedException();
    }
}