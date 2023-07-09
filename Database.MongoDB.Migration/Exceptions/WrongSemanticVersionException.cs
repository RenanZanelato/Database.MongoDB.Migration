namespace Database.MongoDB.Migration.Exceptions;

[Serializable]
internal class WrongSemanticVersionException: Exception
{
    public WrongSemanticVersionException(string name, string version)
        : base($"Migration {name} with version {version} is in wrong format, the correct format should be x.x.x")
    {
    }
}