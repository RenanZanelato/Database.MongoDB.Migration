using System.Threading.Tasks;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Migration
{
    public abstract class BaseMigration
    {
        public abstract string Version { get; }
        public abstract bool IsUp { get; }
        public abstract Task UpAsync(IMongoDatabase database);
        public abstract Task DownAsync(IMongoDatabase database);
    }
}