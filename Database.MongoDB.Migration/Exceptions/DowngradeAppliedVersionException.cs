using System;

namespace Database.MongoDB.Migration.Exceptions
{
    [Serializable]
    internal class DowngradeAppliedVersionException : MigrationException
    {
        public DowngradeAppliedVersionException(string latestToDowngradeName, string latestToDowngradeVersion, string latestAppliedName, string latestAppliedVersion)
            : base($"You have a version {latestAppliedVersion} already applied {latestAppliedName}, that is greater than you will do a downgrade. Your downgrade {latestToDowngradeName} - {latestToDowngradeVersion} must respect the order to apply")
        { }
    }
}