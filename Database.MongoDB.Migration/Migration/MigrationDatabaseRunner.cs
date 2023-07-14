using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;

namespace Database.MongoDB.Migration.Migration
{
    internal class MigrationDatabaseRunner<TMongoInstance> : IMigrationDatabaseRunner<TMongoInstance>
        where TMongoInstance : IMongoMultiInstance
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger<MigrationDatabaseRunner<TMongoInstance>> _logger;
        private readonly IMongoCollection<MigrationDocument> _collection;

        public MigrationDatabaseRunner(IMongoMigrationDatabaseService<TMongoInstance> database,
            ILogger<MigrationDatabaseRunner<TMongoInstance>> logger)
        {
            _mongoDatabase = database.GetDatabase();
            _logger = logger;
            _collection = _mongoDatabase.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        }

        public async Task RunMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations, IEnumerable<MigrationDocument> appliedMigrations) where TMigrations : BaseMigration
        {
            var appliedVersions = appliedMigrations.Select(doc => doc.Version);

            await UpgradeMigrationsAsync(migrations, appliedVersions);
            await DowngradeMigrationsAsync(migrations, appliedVersions);
        }

        private async Task UpgradeMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<string> appliedVersions) where TMigrations : BaseMigration
        {
            var migrationsToUpgrade = migrations
                .Where(m => !appliedVersions.Contains(m.Version) && m.IsUp)
                .OrderBy(m => m.Version);

            foreach (var migration in migrationsToUpgrade)
            {
                await UpgradeMigrationAsync(migration);
            }
        }

        private async Task DowngradeMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<string> appliedVersions) where TMigrations : BaseMigration
        {
            var migrationsToDowngrade = migrations
                .Where(m => appliedVersions.Contains(m.Version) && !m.IsUp)
                .OrderByDescending(m => m.Version);

            foreach (var migration in migrationsToDowngrade)
            {
                await DowngradeMigrationAsync(migration);
            }
        }

        private async Task UpgradeMigrationAsync<TMigrations>(TMigrations migration) where TMigrations : BaseMigration
        {
            await migration.UpAsync(_mongoDatabase);
            var migrationDocument = new MigrationDocument()
            {
                Id = Guid.NewGuid(),
                Name = migration.GetMigrationName(),
                Version = migration.Version,
                CreatedDate = DateTime.UtcNow
            };
            await _collection.InsertOneAsync(migrationDocument);
            _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Up Successfully");
        }

        private async Task DowngradeMigrationAsync<TMigrations>(TMigrations migration) where TMigrations : BaseMigration
        {
            await migration.DownAsync(_mongoDatabase);
            await _collection.DeleteOneAsync(
                Builders<MigrationDocument>.Filter.Where(x => x.Version == migration.Version));
            _logger.LogInformation(
                $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Down Successfully");
        }
    }
}