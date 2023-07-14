using System.Linq;
using System.Threading.Tasks;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Service
{
    internal class MigrationDatabaseService<TMongoInstance> : IMigrationDatabaseService<TMongoInstance>
        where TMongoInstance : IMongoMultiInstance
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMigrationValidator _validator;
        private readonly ILogger<MigrationDatabaseService<TMongoInstance>> _logger;
        private readonly IMigrationDatabaseRunner<TMongoInstance> _runner;
        private readonly MigrationSettings<TMongoInstance> _settings;
        private readonly IMongoCollection<MigrationDocument> _collection;

        public MigrationDatabaseService(IMongoMigrationDatabaseService<TMongoInstance> database,
            IOptions<MigrationSettings<TMongoInstance>> options,
            IMigrationValidator validator,
            ILogger<MigrationDatabaseService<TMongoInstance>> logger,
            IMigrationDatabaseRunner<TMongoInstance> runner)
        {
            _validator = validator;
            _logger = logger;
            _runner = runner;
            _settings = options.Value;
            _mongoDatabase = database.GetDatabase();
            _collection = _mongoDatabase.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        }

        public async Task ExecuteAsync()
        {
            var migrationsToApply = _settings.GetMigrationsFromAssembly();
            if (!migrationsToApply.Any())
            {
                _logger.LogInformation(
                    $"[{_mongoDatabase.DatabaseNamespace.DatabaseName}] Any migrations was found to apply");
                return;
            }

            _validator.IsValidToMigrate(migrationsToApply);

            var appliedMigrations = await _collection.Find(Builders<MigrationDocument>.Filter.Empty).ToListAsync();

            if (appliedMigrations.Any() && _validator.CompareLastedVersionApplied(migrationsToApply, appliedMigrations))
            {
                var migrationApplied = appliedMigrations
                    .OrderBy(x => x.Version)
                    .Last();
                _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}] Latested migration {migrationApplied.Name} version {migrationApplied.Version} already applied");
                return;
            }
            
            await _runner.RunMigrationsAsync(migrationsToApply, appliedMigrations);

        }
    }
}