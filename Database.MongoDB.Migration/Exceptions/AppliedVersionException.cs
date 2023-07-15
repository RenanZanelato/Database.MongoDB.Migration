using System;

namespace Database.MongoDB.Migration.Exceptions
{
    [Serializable]
    internal  class AppliedVersionException : Exception
    {
        public AppliedVersionException(string toApplyVersion, string toApplyName, string appliedVersion)
            : base($"You can't apply {toApplyName} on version {toApplyVersion}, Your version need to be greater than {appliedVersion}")
        { }
    }
}