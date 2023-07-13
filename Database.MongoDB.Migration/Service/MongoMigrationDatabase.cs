using MongoDB.Driver;
using Database.MongoDB.Migration.Interfaces;

namespace Database.MongoDB.Migration.Service
{
    internal class MongoMigrationDatabase<TMongoInstance> : IMongoMigrationDatabase<TMongoInstance> where TMongoInstance : IMongoMultiInstance
    {
        private readonly IMongoDatabase _mongoDatabase;

        public MongoMigrationDatabase(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
        }

        public IMongoDatabase GetDatabase()
        {
            return _mongoDatabase;
        }
    }
}