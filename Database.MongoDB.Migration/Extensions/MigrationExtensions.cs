using MongoDB.Driver;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Extensions;

internal static class MigrationExtensions
{
    internal const string COLLECTION_NAME = "_migrations";
    internal static IEnumerable<BaseMigration> GetMigrationsFromAssembly<TMongoInstance>(this MigrationSettings<TMongoInstance> settings) 
        where TMongoInstance : IMongoMultiInstance
    {
        var migrationTypes = settings.MigrationAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseMigration)) && !t.IsAbstract);

        if (!string.IsNullOrEmpty(settings.Namespace))
        {
           migrationTypes = migrationTypes.Where(x => x.Namespace == settings.Namespace);
        }

        foreach (var type in migrationTypes)
        {
            if (Activator.CreateInstance(type) is BaseMigration migration)
            {
                yield return migration;
            }
        }
    }

    internal static string GetMigrationName(this BaseMigration migration)
        => migration.GetType().Name;
}