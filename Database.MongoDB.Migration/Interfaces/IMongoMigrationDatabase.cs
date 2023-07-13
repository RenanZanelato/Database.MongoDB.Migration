using MongoDB.Driver;

namespace Database.MongoDB.Migration.Interfaces
{
    internal interface IMongoMigrationDatabase<out TMongoInstance> where TMongoInstance : IMongoMultiInstance
    {
        IMongoDatabase GetDatabase();
    }
}