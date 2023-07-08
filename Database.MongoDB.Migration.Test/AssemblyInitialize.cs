using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NUnit.Framework;

namespace Database.MongoDB.Migration.Test;

[SetUpFixture]
public class AssemblyInitialize
{
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var mongoClient = new MongoClient(configuration.GetConnectionString("MongoDB"));
        
        var databases = await mongoClient.ListDatabaseNamesAsync();
        foreach (var database in databases.ToList().Where(x => Guid.TryParse(x, out _)))
        {
            await mongoClient.DropDatabaseAsync(database);
        }

    }
}