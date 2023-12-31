using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database2;

public class PersonSeed : BaseMigration
{
    public override string Version => "2.0.1";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        await collection.InsertManyAsync(PersonFake.CreatePersons(), cancellationToken: cancellationToken);
    }

    public override async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(Builders<Person>.Filter.Empty, cancellationToken);
    }
}