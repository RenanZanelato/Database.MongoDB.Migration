using Database.MongoDB.Migration.Exceptions;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Service;

internal class MigrationValidator : IMigrationValidator
{
    public void IsValidToMigrate<TMigrations>(IEnumerable<TMigrations> migrations) where TMigrations : BaseMigration
    {
        ValidateSemanticVersions(migrations);
        ValidateRepeatedVersions(migrations);
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

    private void ValidateSemanticVersions<TMigrations>(IEnumerable<TMigrations> migrations)
        where TMigrations : BaseMigration
    {
        foreach (var migration in migrations)
        {
            var versionSeparator = migration.Version.Split(".");
            var migrationName = migration.GetMigrationName();
            if (versionSeparator.Length != 3)
            {
                throw new WrongSemanticVersionException(migrationName, migration.Version);
            }

            ValidateVersionNumber(migrationName, versionSeparator[0]);
            ValidateVersionNumber(migrationName, versionSeparator[1]);
            ValidateVersionNumber(migrationName, versionSeparator[2]);
        }
    }
    
    private void ValidateVersionNumber(string migrationName, string value)
    {
        if (!int.TryParse(value, out _))
        {
            throw new WrongVersionException(migrationName, value);
        }
    }
}