using Database.MongoDB.Migration.Exceptions;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Service;

internal class MigrationValidator : IMigrationValidator
{
    public void IsValidToMigrate<TMigrations>(IEnumerable<TMigrations> migrations) where TMigrations : BaseMigration
    {
        ValidateRepeatedVersions(migrations);
        ValidateSequentialVersions(migrations);
    }

    private void ValidateRepeatedVersions<TMigrations>(IEnumerable<TMigrations> migrations)
        where TMigrations : BaseMigration
    {
        var groupedMigrations = migrations.GroupBy(x => x.Version);

        foreach (var migration in groupedMigrations)
        {
            if (migration.Count() > 1)
            {
                throw new RepeatedVersionException(migration.Select(x => x.GetMigrationName()), migration.FirstOrDefault().Version);
            }
        }
    }

    private void ValidateSequentialVersions<TMigrations>(IEnumerable<TMigrations> migrations)
        where TMigrations : BaseMigration
    {
        var orderedMigrations = migrations.OrderBy(x => x.Version);
        var previousVersion = 0;

        foreach (var migration in orderedMigrations)
        {
            if (migration.Version != previousVersion + 1)
            {
                throw new SequentialVersionException(migration.GetMigrationName(), migration.Version);
            }
        
            previousVersion = migration.Version;
        }

    }
}