namespace Database.MongoDB.Migration.Exceptions;

[Serializable]
internal class WrongVersionException: Exception
{
    public WrongVersionException(string name, string version)
        : base($"Migration {name} with version {version} has wrong number of version. All parts need to be a number")
    {
    }
}