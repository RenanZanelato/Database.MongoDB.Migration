using MongoDB.Driver;

namespace Database.MongoDB.Migration.Interfaces;

internal interface IMigrationDatabaseRunner<in TMongoInstance> where TMongoInstance : IMongoMultiInstance 
{
    Task RunMigrationsAsync();
}