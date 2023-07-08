namespace Database.MongoDB.Migration.Exceptions;

[Serializable]
internal class VersionException: Exception
{
    public VersionException(IEnumerable<string> name, int version)
        : base($"The files {string.Join(", ", name)} has repeated version {version}")
    {
    }
}