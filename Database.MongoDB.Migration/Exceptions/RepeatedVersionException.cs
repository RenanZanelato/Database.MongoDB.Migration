using System;
using System.Collections.Generic;

namespace Database.MongoDB.Migration.Exceptions
{
    [Serializable]
    internal class RepeatedVersionException: MigrationException
    {
        public RepeatedVersionException(IEnumerable<string> names, string version)
            : base($"Migrations {string.Join(", ", names)} has repeated version {version}")
        {
        }
    }
}