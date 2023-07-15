using System;

namespace Database.MongoDB.Migration.Exceptions
{
    [Serializable]
    internal class WrongVersionException: MigrationException
    {
        public WrongVersionException(string name, string version, string wrongPath)
            : base($"Migration {name} with version {version} has wrong path {wrongPath}. All parts need to be a number")
        {
        }
    }
}