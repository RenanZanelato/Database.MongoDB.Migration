using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Migration
{
    public abstract class BaseMigration
    {
        public abstract string Version { get; }
        public abstract bool IsUp { get; }
        public abstract Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default);
        public abstract Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default);
    }
}