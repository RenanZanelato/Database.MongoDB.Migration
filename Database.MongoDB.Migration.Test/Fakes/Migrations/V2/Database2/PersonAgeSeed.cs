using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database2;

public class PersonAgeSeed : BaseMigration
{
    public override string Version => "2.0.2";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(x => x.Id == PersonFake.DefaultPersonId, cancellationToken: cancellationToken))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        person.Age = PersonFake.DefaultPersonAge;

        await collection.ReplaceOneAsync(x => x.Id == PersonFake.DefaultPersonId, person, cancellationToken: cancellationToken);
    }

    public override async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(x => x.Id == PersonFake.DefaultPersonId, cancellationToken: cancellationToken))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        person.Age = 0;

        await collection.ReplaceOneAsync(x => x.Id == PersonFake.DefaultPersonId, person, cancellationToken: cancellationToken);
    }
}