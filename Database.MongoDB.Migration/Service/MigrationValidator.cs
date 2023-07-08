using Database.MongoDB.Migration.Exceptions;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Service;

internal class MigrationValidator : IMigrationValidator
{
    public void IsValidToMigrate<TMigrations>(IEnumerable<TMigrations> migrations) where TMigrations : BaseMigration
    {
        var groupedMigrations = migrations.GroupBy(x => x.Version);
        foreach (var migration in groupedMigrations)
        {
            if (migration.Count() > 1)
            {
                throw new VersionException(migration.Select(x => x.GetMigrationName()), migration.FirstOrDefault().Version);
            }
        }
    }
}