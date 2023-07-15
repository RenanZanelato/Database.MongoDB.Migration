using System.Collections.Generic;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Interfaces
{
    internal interface IMigrationValidator
    {
        void ValidateMigrations<TMigrations>(IEnumerable<TMigrations> migrations) where TMigrations : BaseMigration;
        void ValidateMigrations<TMigrations>(IEnumerable<TMigrations> migrations, IEnumerable<MigrationDocument> migrationsApplied) where TMigrations : BaseMigration;
        bool CompareLastedVersionApplied<TMigrations>(IEnumerable<TMigrations> migrations, IEnumerable<MigrationDocument> migrationsApplied) where TMigrations : BaseMigration;
    }
}