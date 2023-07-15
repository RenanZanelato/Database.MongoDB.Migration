using System;

namespace Database.MongoDB.Migration.Exceptions
{
    [Serializable]
    internal class DowngradeVersionException : MigrationException
    {
        public DowngradeVersionException(string latestToDowngradeName, string latestToDowngradeVersion, string latestToUpgradeName, string latestToUpgradeVersion)
            : base($"You need first apply a downgrade on {latestToDowngradeName} version {latestToDowngradeVersion} to before apply a upgrade on {latestToUpgradeName} version {latestToUpgradeVersion}")
        { }
    }
}