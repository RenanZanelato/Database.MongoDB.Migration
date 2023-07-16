using System.Collections.Generic;
using System.Linq;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Exceptions;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;

namespace Database.MongoDB.Migration.Validator
{
    internal class MigrationValidator : IMigrationValidator
    {
        public void ValidateMigrations<TMigrations>(IEnumerable<TMigrations> migrations)
            where TMigrations : BaseMigration
        {
            ValidateSemanticVersions(migrations);
            ValidateRepeatedVersions(migrations);
        }

        public void ValidateMigrations<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<MigrationDocument> migrationsApplied)
            where TMigrations : BaseMigration
        {
            var appliedVersions = migrationsApplied.Select(x => x.Version);
            var migrationsToUpgrade = migrations.GetMigrationsToUpgrade(appliedVersions);

            if (!migrationsApplied.Any() || !migrationsToUpgrade.Any())
            {
                return;
            }

            var latestToUpgrade = migrationsToUpgrade.OrderByDescending(x => x.Version).FirstOrDefault();
            var latestApplied = migrationsApplied.OrderByDescending(x => x.Version).FirstOrDefault();

            ValidateAppliedVersion(latestToUpgrade, latestApplied);
        }
        
        public bool ValidateLastedVersionApplied<TMigrations>(IEnumerable<TMigrations> migrations,
            IEnumerable<MigrationDocument> migrationsApplied) where TMigrations : BaseMigration
        {
            var latestMigrationToApply = migrations
                .Where(x => x.IsUp)
                .OrderBy(x => x.Version)
                .Last();

            var latestMigrationApplied = migrationsApplied
                .OrderBy(x => x.Version)
                .Last();

            return latestMigrationToApply.Version == latestMigrationApplied.Version;
        }

        private void ValidateAppliedVersion<TMigrations>(TMigrations latestToUpgrade, MigrationDocument latestApplied)
            where TMigrations : BaseMigration
        {
            if (latestApplied.Version.GetVersion() <= latestToUpgrade.Version.GetVersion())
            {
                throw new AppliedVersionException(latestToUpgrade.Version, latestToUpgrade.GetMigrationName(),
                    latestApplied.Version);
            }
        }
        
        private void ValidateRepeatedVersions<TMigrations>(IEnumerable<TMigrations> migrations)
            where TMigrations : BaseMigration
        {
            var repeatedVersions = migrations
                .GroupBy(x => x.Version)
                .Where(g => g.Count() > 1)
                .Select(g => new { Names = g.Select(x => x.GetMigrationName()), Version = g.Key });

            if (repeatedVersions.Any())
            {
                throw new RepeatedVersionException(repeatedVersions.SelectMany(x => x.Names), repeatedVersions.First().Version);
            }
        }

        private void ValidateSemanticVersions<TMigrations>(IEnumerable<TMigrations> migrations)
            where TMigrations : BaseMigration
        {
            foreach (var migration in migrations)
            {
                var versionSeparator = migration.Version.Split('.');
                if (versionSeparator.Length != 3)
                {
                    throw new WrongSemanticVersionException(migration.GetMigrationName(), migration.Version);
                }

                ValidateVersionNumber(migration, versionSeparator[0]);
                ValidateVersionNumber(migration, versionSeparator[1]);
                ValidateVersionNumber(migration, versionSeparator[2]);
            }
        }

        private void ValidateVersionNumber<TMigrations>(TMigrations migration, string value)
            where TMigrations : BaseMigration
        {
            if (!int.TryParse(value, out _))
            {
                throw new WrongVersionException(migration.GetMigrationName(), migration.Version, value);
            }
        }
    }
}