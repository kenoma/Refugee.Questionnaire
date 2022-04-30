using LiteDB;
using RQ.DTO;

namespace Bot.Repo;

public class LiteDbRepo : IRepository
{
    private readonly string _dbPath;

    public LiteDbRepo(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    public bool TryGetChatData(long chatId, out ChatMetadata chatConfig)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<ChatMetadata>(nameof(ChatMetadata));

        collection.EnsureIndex(x => x.ChatId, unique: true);

        chatConfig = collection.FindOne(z => z.ChatId == chatId);

        return chatConfig != null;
    }

    public bool IsKnownToken(string value)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<Volunteer>(nameof(Volunteer));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);
        
        return collection.FindOne(z => z.Token == value) != null;
    }

    public bool IsKnownTgUser(long userId)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<Volunteer>(nameof(Volunteer));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);

        return collection.FindOne(z => z.UserId == userId) != null;
    }

    public Questionnaire[] GetAllQuestionaries()
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<Questionnaire>(nameof(Questionnaire));

        return collection.FindAll().ToArray();
    }
}