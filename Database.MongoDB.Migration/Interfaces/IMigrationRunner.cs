using System.Collections.Generic;
using System.Threading.Tasks;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Interfaces
{
    internal interface IMigrationDatabaseRunner<in TMongoInstance> where TMongoInstance : IMongoMultiInstance 
    {
        Task RunMigrationsAsync(IEnumerable<BaseMigration> migrations, IEnumerable<MigrationDocument> appliedMigrations);
    }
}