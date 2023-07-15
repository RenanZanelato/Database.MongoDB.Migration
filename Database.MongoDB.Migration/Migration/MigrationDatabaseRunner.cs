using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public MigrationDatabaseRunner(IMongoMigrationDatabaseService<TMongoInstance> database, ILogger<MigrationDatabaseRunner<TMongoInstance>> logger)
        {
            _mongoDatabase = database.GetDatabase();
            _logger = logger;
            _collection = _mongoDatabase.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        }

        public async Task RunMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<MigrationDocument> appliedMigrations, CancellationToken cancellationToken = default) where TMigrations : BaseMigration
        {
            var appliedVersions = appliedMigrations.Select(doc => doc.Version);

            await UpgradeMigrationsAsync(migrations, appliedVersions, cancellationToken);
            await DowngradeMigrationsAsync(migrations, appliedVersions, cancellationToken);
        }

        private async Task UpgradeMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<string> appliedVersions, CancellationToken cancellationToken = default) where TMigrations : BaseMigration
        {
            var migrationsToUpgrade = migrations
                .Where(m => !appliedVersions.Contains(m.Version) && m.IsUp)
                .OrderBy(m => m.Version);

            foreach (var migration in migrationsToUpgrade)
            {
                await UpgradeMigrationAsync(migration, cancellationToken);
            }
        }

        private async Task DowngradeMigrationsAsync<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<string> appliedVersions, CancellationToken cancellationToken = default) where TMigrations : BaseMigration
        {
            var migrationsToDowngrade = migrations
                .Where(m => appliedVersions.Contains(m.Version) && !m.IsUp)
                .OrderByDescending(m => m.Version);

            foreach (var migration in migrationsToDowngrade)
            {
                await DowngradeMigrationAsync(migration, cancellationToken);
            }
        }

        private async Task UpgradeMigrationAsync<TMigrations>(TMigrations migration,
            CancellationToken cancellationToken = default) where TMigrations : BaseMigration
        {
            await migration.UpAsync(_mongoDatabase, cancellationToken);
            var migrationDocument = new MigrationDocument()
            {
                Id = Guid.NewGuid(),
                Name = migration.GetMigrationName(),
                Version = migration.Version,
                CreatedDate = DateTime.UtcNow
            };
            await _collection.InsertOneAsync(migrationDocument, cancellationToken: cancellationToken);
            _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Up Successfully");
        }

        private async Task DowngradeMigrationAsync<TMigrations>(TMigrations migration,
            CancellationToken cancellationToken = default) where TMigrations : BaseMigration
        {
            await migration.DownAsync(_mongoDatabase, cancellationToken);
            await _collection.DeleteOneAsync(
                Builders<MigrationDocument>.Filter.Where(x => x.Version == migration.Version), cancellationToken);
            _logger.LogInformation(
                $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Down Successfully");
        }
    }
}