using System.Reflection;
using Database.MongoDB.Migration.Interfaces;

namespace Database.MongoDB.Migration;

public class MigrationSettings<TMongoInstance> where TMongoInstance : IMongoMultiInstance
{
    public Assembly MigrationAssembly { get; set; }
    public string Namespace { get; set; }
}