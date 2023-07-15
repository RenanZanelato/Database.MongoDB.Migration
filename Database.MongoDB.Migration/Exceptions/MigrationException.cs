using System;

namespace Database.MongoDB.Migration.Exceptions
{
    internal class MigrationException : Exception
    {
        protected MigrationException(string message) : base(message)
        {
            
        }
    }
}