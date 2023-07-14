using MongoDB.Driver;

namespace Database.MongoDB.Migration.Interfaces
{
    internal interface IMongoMigrationDatabaseService<out TMongoInstance> where TMongoInstance : IMongoMultiInstance
    {
        IMongoDatabase GetDatabase();
    }
}