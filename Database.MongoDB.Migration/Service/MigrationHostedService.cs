using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;

namespace Database.MongoDB.Migration.Service
{
    internal class MigrationHostedService<TMongoInstance> : BackgroundService
        where TMongoInstance : IMongoMultiInstance
    {
        private readonly IMigrationDatabaseService<TMongoInstance> _databaseService;
        private readonly IMongoDatabase _mongoDatabase;

        public MigrationHostedService(IMigrationDatabaseService<TMongoInstance> databaseService, IMongoMigrationDatabase<TMongoInstance> database)
        {
            _databaseService = databaseService;
            _mongoDatabase = database.GetDatabase();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CreateMigrationDocumentIndex(stoppingToken);
            await _databaseService.ExecuteAsync();
        }

        private async Task CreateMigrationDocumentIndex(CancellationToken stoppingToken)
        {
            var collection = _mongoDatabase.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
            var index = new CreateIndexModel<MigrationDocument>(Builders<MigrationDocument>.IndexKeys
                .Ascending(x => x.Version), new CreateIndexOptions()
            {
                Name = "INDEX_01",
                Unique = true
            });
            await collection.Indexes.CreateOneAsync(index, cancellationToken: stoppingToken);
        }
    }
}