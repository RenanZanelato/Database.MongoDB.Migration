using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V1;

public class PersonSeed : BaseMigration
{
    public override string Version => "1.0.1";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        await collection.InsertManyAsync(clientSessionHandle, PersonFake.CreatePersons());
    }

    public override async Task DownAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(clientSessionHandle, Builders<Person>.Filter.Empty);
    }
}