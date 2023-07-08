namespace Database.MongoDB.Migration.Exceptions;

[Serializable]
internal class SequentialVersionException: Exception
{
    public SequentialVersionException(string name, int version)
        : base($"Migration {name} has a wrong sequential version {version}")
    {
    }
}