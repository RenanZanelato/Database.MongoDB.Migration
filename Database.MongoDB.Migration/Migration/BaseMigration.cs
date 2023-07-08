using MongoDB.Driver;

namespace Database.MongoDB.Migration.Migration;

public abstract class BaseMigration
{
    public abstract int Version { get; }
    public abstract bool IsUp { get; }
    public abstract Task UpAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database);
    public abstract Task DownAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database);
}