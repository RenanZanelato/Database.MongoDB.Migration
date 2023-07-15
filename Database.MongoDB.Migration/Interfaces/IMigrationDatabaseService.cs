using System.Threading;
using System.Threading.Tasks;

namespace Database.MongoDB.Migration.Interfaces
{
    internal interface IMigrationDatabaseService<in TMongoInstance> where TMongoInstance : IMongoMultiInstance
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}