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
            if (!migrations.Any())
            {
                _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}] Any migrations was found to apply");
                return;
            }
            
            _validator.IsValidToMigrate(migrations);

            var appliedMigrations = await _collection.Find(Builders<MigrationDocument>.Filter.Empty).ToListAsync();
            if (appliedMigrations.Any() && _validator.CompareLastedVersionApplied(migrations, appliedMigrations))
            {
                var migrationApplied = appliedMigrations
                    .OrderBy(x => x.Version)
                    .Last();
                _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}] Latested migration {migrationApplied.Name} version {migrationApplied.Version} already applied");
                return;
            }
            
            var appliedVersions = appliedMigrations.Select(doc => doc.Version);

            await UpgradeMigrationsAsync(migrations, appliedVersions);
            await DowngradeMigrationsAsync(migrations, appliedVersions);
        }

        private async Task UpgradeMigrationsAsync(IEnumerable<BaseMigration> migrations,
            IEnumerable<string> appliedVersions)
        {
            var migrationsToUpgrade = migrations
                .Where(m => !appliedVersions.Contains(m.Version) && m.IsUp)
                .OrderBy(m => m.Version);

            foreach (var migration in migrationsToUpgrade)
            {
                await UpgradeMigrationAsync(migration);
            }
        }

        private async Task DowngradeMigrationsAsync(IEnumerable<BaseMigration> migrations,
            IEnumerable<string> appliedVersions)
        {
            var migrationsToDowngrade = migrations
                .Where(m => appliedVersions.Contains(m.Version) && !m.IsUp)
                .OrderByDescending(m => m.Version);

            foreach (var migration in migrationsToDowngrade)
            {
                await DowngradeMigrationAsync(migration);
            }
        }

        private async Task UpgradeMigrationAsync(BaseMigration migration)
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

        private async Task DowngradeMigrationAsync(BaseMigration migration)
        {
            await migration.DownAsync(_mongoDatabase);
            await _collection.DeleteOneAsync(
                Builders<MigrationDocument>.Filter.Where(x => x.Version == migration.Version));
            _logger.LogInformation(
                $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Down Successfully");
        }
    }
}