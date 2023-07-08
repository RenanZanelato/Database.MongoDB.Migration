using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Interfaces;

internal interface IMigrationValidator
{
    void IsValidToMigrate<TMigrations>(IEnumerable<TMigrations> migrations) where TMigrations : BaseMigration;
}