using System.Collections.Generic;
using System.Threading.Tasks;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Interfaces
{
    internal interface IMigrationDatabaseRunner<in TMongoInstance> where TMongoInstance : IMongoMultiInstance
    {
        Task RunMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<MigrationDocument> appliedMigrations) where TMigrations : BaseMigration;
    }
}