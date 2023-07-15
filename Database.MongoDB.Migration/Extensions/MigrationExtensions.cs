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
            var migrationTypes = (settings.MigrationAssembly ?? Assembly.GetExecutingAssembly())
                .GetTypes()
                .Where(IsValidMigrationType(settings.Namespace))
                .ToList();

            return migrationTypes.Select(Activator.CreateInstance)
                .OfType<BaseMigration>();
        }

        internal static string GetMigrationName(this BaseMigration migration)
            => migration.GetType().Name;
        
        internal static int GetVersion(this string migrationVersion)
            => int.Parse(migrationVersion.Replace(".", ""));
        
        internal static IEnumerable<TMigrations> GetMigrationsToUpgrade<TMigrations>(this IEnumerable<TMigrations> migrations, IEnumerable<string> appliedVersions)
            where TMigrations : BaseMigration
        => migrations
            .Where(m => !appliedVersions.Contains(m.Version) && m.IsUp);
        
        internal static IEnumerable<TMigrations> GetMigrationsToDowngrade<TMigrations>(this IEnumerable<TMigrations> migrations, IEnumerable<string> appliedVersions)
            where TMigrations : BaseMigration
        => migrations
            .Where(m => appliedVersions.Contains(m.Version) && !m.IsUp);
        
        private static Func<Type, bool> IsValidMigrationType(string migrationNamespace) =>
            t => t.IsSubclassOf(typeof(BaseMigration)) &&
                 !t.IsAbstract &&
                 !string.IsNullOrEmpty(migrationNamespace) &&
                 t.Namespace == migrationNamespace;
    }
}