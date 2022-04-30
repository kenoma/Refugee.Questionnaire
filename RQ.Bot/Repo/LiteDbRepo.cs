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

    public RefRequest[] GetAllRequest()
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));

        return collection.FindAll().ToArray();
    }

    public RefRequest[] GetAllRequestFromUser(long userId)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.UserId);

        return collection.Find(z => z.UserId == userId).ToArray();
    }

    public RefRequest GetRequest(Guid requestId)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.Id);

        return collection.FindOne(z => z.Id == requestId);
    }

    public void UpdateRefRequest(RefRequest request)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.Id);

        collection.Upsert(request);
    }

    public bool TryGetActiveUserRequest(long userId, out RefRequest refRequest)
    {
        using var db = new LiteDatabase(_dbPath);

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.Id);

        refRequest = collection.FindOne(z => !z.IsCompleted);
        return refRequest != null;
    }
}