using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IMigrationValidator _validator;
        private readonly ILogger<MigrationDatabaseRunner<TMongoInstance>> _logger;
        private readonly MigrationSettings<TMongoInstance> _settings;
        private readonly IMongoCollection<MigrationDocument> _collection;

        public MigrationDatabaseRunner(IMongoMigrationDatabase<TMongoInstance> database,
            IOptions<MigrationSettings<TMongoInstance>> options,
            IMigrationValidator validator,
            ILogger<MigrationDatabaseRunner<TMongoInstance>> logger)
        {
            _mongoDatabase = database.GetDatabase();
            _validator = validator;
            _logger = logger;
            _settings = options.Value;
            _collection = _mongoDatabase.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        }

        public async Task RunMigrationsAsync()
        {
            var migrations = _settings.GetMigrationsFromAssembly();
            _validator.IsValidToMigrate(migrations);

            var appliedMigrations = await _collection.Find(Builders<MigrationDocument>.Filter.Empty).ToListAsync();
            var appliedVersions = appliedMigrations.Select(doc => doc.Version);

            using (var session = await _mongoDatabase.Client.StartSessionAsync())
            {
                try
                {
                    session.StartTransaction();

                    await UpgradeMigrationsAsync(session, migrations, appliedVersions);
                    await DowngradeMigrationsAsync(session, migrations, appliedVersions);

                    await session.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await session.AbortTransactionAsync();
                    _logger.LogError(ex, $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}] Migrations failed. Transaction aborted.");
                }
                finally
                {
                    session.Dispose();
                }
            }
        }

        private async Task UpgradeMigrationsAsync(IClientSessionHandle clientSessionHandle,
            IEnumerable<BaseMigration> migrations,
            IEnumerable<string> appliedVersions)
        {
            var migrationsToUpgrade = migrations
                .Where(m => !appliedVersions.Contains(m.Version) && m.IsUp)
                .OrderBy(m => m.Version);

            foreach (var migration in migrationsToUpgrade)
            {
                await UpgradeMigrationAsync(clientSessionHandle, migration);
            }
        }

        private async Task DowngradeMigrationsAsync(IClientSessionHandle clientSessionHandle,
            IEnumerable<BaseMigration> migrations,
            IEnumerable<string> appliedVersions)
        {
            var migrationsToDowngrade = migrations
                .Where(m => appliedVersions.Contains(m.Version) && !m.IsUp)
                .OrderByDescending(m => m.Version);

            foreach (var migration in migrationsToDowngrade)
            {
                await DowngradeMigrationAsync(clientSessionHandle, migration);
            }
        }

        private async Task UpgradeMigrationAsync(IClientSessionHandle clientSessionHandle, BaseMigration migration)
        {
            await migration.UpAsync(clientSessionHandle, _mongoDatabase);
            var migrationDocument = new MigrationDocument()
            {
                Id = Guid.NewGuid(),
                Name = migration.GetMigrationName(),
                Version = migration.Version,
                CreatedDate = DateTime.UtcNow
            };
            await _collection.InsertOneAsync(migrationDocument);
            _logger.LogInformation(
                $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Up Successfully");
        }

        private async Task DowngradeMigrationAsync(IClientSessionHandle clientSessionHandle,
            BaseMigration migration)
        {
            await migration.DownAsync(clientSessionHandle, _mongoDatabase);
            await _collection.DeleteOneAsync(
                Builders<MigrationDocument>.Filter.Where(x => x.Version == migration.Version));
            _logger.LogInformation(
                $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Down Successfully");
        }
    }
}