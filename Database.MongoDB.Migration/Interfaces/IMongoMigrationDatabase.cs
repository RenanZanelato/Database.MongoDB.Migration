using MongoDB.Driver;

namespace Database.MongoDB.Migration.Interfaces;

public interface IMongoMigrationDatabase<out TMongoInstance> where TMongoInstance : IMongoMultiInstance
{
    IMongoDatabase GetDatabase();
}