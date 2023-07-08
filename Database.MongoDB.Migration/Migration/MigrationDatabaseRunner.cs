using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;

namespace Database.MongoDB.Migration.Migration;

internal class MigrationDatabaseRunner<TMongoInstance> : IMigrationDatabaseRunner<TMongoInstance> where TMongoInstance : IMongoMultiInstance
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IMigrationValidator _validator;
    private readonly ILogger<MigrationDatabaseRunner<TMongoInstance>> _logger;
    private readonly MigrationSettings<TMongoInstance> _settings;
    
    public MigrationDatabaseRunner(IMongoMigrationDatabase<TMongoInstance> database, 
        IOptions<MigrationSettings<TMongoInstance>> options,
        IMigrationValidator validator,
        ILogger<MigrationDatabaseRunner<TMongoInstance>> logger)
    {
        _mongoDatabase = database.GetDatabase();
        _validator = validator;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task RunMigrationsAsync()
    {
        var migrations = _settings.GetMigrationsFromAssembly();

        var migrationCollection = _mongoDatabase.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);

        var appliedMigrations = await migrationCollection.Find(Builders<MigrationDocument>.Filter.Empty).ToListAsync();
        var appliedVersions = appliedMigrations.Select(doc => doc.Version);

        migrations = migrations
            .OrderBy(m => m.Version)
            .Where(m => (!appliedVersions.Contains(m.Version) && m.IsUp) || (appliedVersions.Contains(m.Version) && !m.IsUp));
    
        _validator.IsValidToMigrate(migrations);
        
        using var session = await _mongoDatabase.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            foreach (var migration in migrations)
            {
                await UpgradeOrDowngradeMigrationAsync(session, migrationCollection, migration);
            }

            await session.CommitTransactionAsync();
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync();
            _logger.LogError(e, "Migration failed. Transaction aborted.");
        }
        finally
        {
            session.Dispose();
        }
    }

    private async Task UpgradeOrDowngradeMigrationAsync(IClientSessionHandle session, IMongoCollection<MigrationDocument> migrationCollection, BaseMigration migration)
    {
        if (migration.IsUp)
        {
            await migration.UpAsync(session, _mongoDatabase);
            var migrationDocument = new MigrationDocument()
            {
                Id = Guid.NewGuid(),
                Name = migration.GetMigrationName(),
                Version = migration.Version,
                CreatedDate = DateTime.UtcNow
            };
            await migrationCollection.InsertOneAsync(session, migrationDocument);
            _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Up Successfully");
            return;
        }
        await migration.DownAsync(session, _mongoDatabase);
        await migrationCollection.DeleteOneAsync(session, Builders<MigrationDocument>.Filter.Where(x => x.Version == migration.Version && x.Name == nameof(migration)));
        _logger.LogInformation($"[{_mongoDatabase.DatabaseNamespace.DatabaseName}][{migration.GetMigrationName()}][{migration.Version}] Down Successfully");
    }
}