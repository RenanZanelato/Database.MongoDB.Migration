namespace Database.MongoDB.Migration.Exceptions;

[Serializable]
internal class WrongSemanticVersionException: Exception
{
    public WrongSemanticVersionException(string name, string version)
        : base($"Migration {name} with version {version} has wrong format, correct needs to be: major.minor.patch")
    {
    }
}