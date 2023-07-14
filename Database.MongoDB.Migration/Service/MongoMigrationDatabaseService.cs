using MongoDB.Driver;
using Database.MongoDB.Migration.Interfaces;

namespace Database.MongoDB.Migration.Service
{
    internal class MongoMigrationDatabaseService<TMongoInstance> : IMongoMigrationDatabaseService<TMongoInstance> where TMongoInstance : IMongoMultiInstance
    {
        private readonly IMongoDatabase _mongoDatabase;

        public MongoMigrationDatabaseService(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
        }

        public IMongoDatabase GetDatabase()
        {
            return _mongoDatabase;
        }
    }
}