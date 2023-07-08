using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Service;

namespace Database.MongoDB.Migration;

public static class MongoMigrationExtension
{
    public static IServiceCollection AddMongoMigration(this IServiceCollection serviceCollection,
        IMongoDatabase mongoDatabase)
        => serviceCollection.AddMongoMigration<MongoDefaultInstance>(mongoDatabase);

    public static IServiceCollection AddMongoMigration(this IServiceCollection serviceCollection,
        IMongoDatabase mongoDatabase, Action<MigrationSettings<MongoDefaultInstance>> options)
        => serviceCollection.AddMongoMigration<MongoDefaultInstance>(mongoDatabase, options);

    public static IServiceCollection AddMongoMigration<TMongoInstance>(this IServiceCollection serviceCollection,
        IMongoDatabase mongoDatabase)
        where TMongoInstance : IMongoMultiInstance
        => serviceCollection.AddMongoMigration<TMongoInstance>(mongoDatabase,
            settings => settings.MigrationAssembly = Assembly.GetExecutingAssembly());

    public static IServiceCollection AddMongoMigration<TMongoInstance>(this IServiceCollection serviceCollection,
        IMongoDatabase mongoDatabase,
        Action<MigrationSettings<TMongoInstance>> options)
        where TMongoInstance : IMongoMultiInstance
    {
        serviceCollection
            .AddOptions<MigrationSettings<TMongoInstance>>()
            .Configure(options);

        serviceCollection
            .AddSingleton<IMigrationValidator, MigrationValidator>()
            .AddSingleton<IMongoMigrationDatabase<TMongoInstance>, MongoMigrationDatabase<TMongoInstance>>(_ =>
                new MongoMigrationDatabase<TMongoInstance>(mongoDatabase))
            .AddSingleton<IMigrationDatabaseRunner<TMongoInstance>, MigrationDatabaseRunner<TMongoInstance>>()
            .AddHostedService<MigrationHostedService<TMongoInstance>>();

        return serviceCollection;
    }
}