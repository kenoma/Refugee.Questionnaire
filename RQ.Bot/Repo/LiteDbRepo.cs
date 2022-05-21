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
        using var db = new LiteDatabase(Path.Combine(_dbPath, "users.ldb"));

        var collection = db.GetCollection<UserData>(nameof(UserData));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);
        
        return collection.FindOne(z => z.Token == value) != null;
    }

    public bool TryGetUserById(long userId, out UserData user)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "users.ldb"));

        var collection = db.GetCollection<UserData>(nameof(UserData));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);

        user = collection.FindOne(z => z.UserId == userId);
        return user != null;
    }

    public RefRequest[] GetAllRequests()
    {
        var getAllArchives = Directory.GetFiles(_dbPath, "*current_requests.ldb");

        var retval = new List<RefRequest>();
        foreach (var archive in getAllArchives)
        {
            using var db = new LiteDatabase(archive);

            var collection = db.GetCollection<RefRequest>(nameof(RefRequest));

            retval.AddRange(collection.FindAll());
        }

        return retval.ToArray();
    }

    public RefRequest[] GetCurrentRequests()
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));

        return collection.FindAll().ToArray();
    }

    public RefRequest[] GetAllRequestFromUser(long userId)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.UserId);

        return collection.Find(z => z.UserId == userId).ToArray();
    }

    public RefRequest GetRequest(Guid requestId)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.Id);

        return collection.FindOne(z => z.Id == requestId);
    }

    public void UpdateRefRequest(RefRequest request)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.Id);

        collection.Upsert(request);
    }

    public bool TryGetActiveUserRequest(long userId, out RefRequest refRequest)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.UserId);

        refRequest = collection.FindOne(z => z.UserId == userId && !z.IsCompleted);
        return refRequest != null;
    }
    
    public UserData[] GetAdminUsers()
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "users.ldb"));

        var collection = db.GetCollection<UserData>(nameof(UserData));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);

        return collection.Find(z => z.IsAdmin).ToArray();
    }

    public UserData[] GetAllUsers()
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "users.ldb"));

        var collection = db.GetCollection<UserData>(nameof(UserData));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);

        return collection.FindAll().ToArray();
    }

    public void UpsertUser(UserData rfUser)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "users.ldb"));

        var collection = db.GetCollection<UserData>(nameof(UserData));
        collection.EnsureIndex(z => z.UserId, unique: true);
        
        collection.Upsert(rfUser);
    }

    public void ArchiveCurrentRequests()
    {
        using var dbSource = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));
        using var dbDestination = new LiteDatabase(Path.Combine(_dbPath, $"{DateTime.Now.Ticks}_current_requests.ldb"));
        
        var collectionSource = dbSource.GetCollection<RefRequest>(nameof(RefRequest));
        var collectionDest = dbDestination.GetCollection<RefRequest>(nameof(RefRequest));

        collectionDest.Insert(collectionSource.FindAll());

        collectionSource.DeleteAll();
    }

    public void RemoveRequest(Guid refRequestId)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));

        collection.Delete(refRequestId);
    }
    
    public RefRequest[] GetRequestsDt(DateTime dt)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "current_requests.ldb"));

        var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
        collection.EnsureIndex(z => z.Id);
        collection.EnsureIndex(z => z.TimeStamp);

        return collection.Find(z => z.TimeStamp > dt).OrderBy(z => z.TimeStamp).ToArray();

      
    }
    public RefRequest[] GetRequestsDtArch(DateTime dt)
    {
        var getAllArchives = Directory.GetFiles(_dbPath, "*current_requests.ldb");

        var retval = new List<RefRequest>();
        foreach (var archive in getAllArchives)
        {
            using var db = new LiteDatabase(archive);

            var collection = db.GetCollection<RefRequest>(nameof(RefRequest));
            collection.EnsureIndex(z => z.Id);
            collection.EnsureIndex(z => z.TimeStamp);

            retval.AddRange(collection.Find(z => z.TimeStamp > dt).OrderBy(z => z.TimeStamp));
        }

        return retval.OrderBy(t => t.TimeStamp).ToArray();
    }
    public UserData[] GetUsersDt(DateTime dt)
    {
        using var db = new LiteDatabase(Path.Combine(_dbPath, "users.ldb"));

        var collection = db.GetCollection<UserData>(nameof(UserData));
        collection.EnsureIndex(z => z.Token);
        collection.EnsureIndex(z => z.UserId, unique: true);
        collection.EnsureIndex(z => z.Created);

        return collection.Find(z => z.Created > dt).OrderBy(z => z.Created).ToArray();
    }
}