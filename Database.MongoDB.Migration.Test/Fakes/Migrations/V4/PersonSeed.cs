using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V4;

public class PersonSeed : BaseMigration
{
    public override int Version => 3;
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        await collection.InsertManyAsync(PersonFake.CreatePersons());
    }

    public override async Task DownAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(Builders<Person>.Filter.Empty);
    }
}