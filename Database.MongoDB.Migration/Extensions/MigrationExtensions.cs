using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Extensions
{
    internal static class MigrationExtensions
    {
        internal const string COLLECTION_NAME = "_migrations";
        internal static IEnumerable<BaseMigration> GetMigrationsFromAssembly<TMongoInstance>(this MigrationSettings<TMongoInstance> settings) 
            where TMongoInstance : IMongoMultiInstance
        {
            var migrationTypes = (settings.MigrationAssembly ?? Assembly.GetExecutingAssembly()).GetTypes()
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
        
        internal static int GetVersion(this string migrationVersion)
            => int.Parse(migrationVersion.Replace(".", ""));
    }
}