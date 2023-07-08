using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V1;

public class PersonAgeSeed : BaseMigration
{
    public override int Version => 2;
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(Builders<Person>.Filter.Where(x => x.Id == PersonFake.DefaultPersonId))).FirstOrDefaultAsync();
        person.Age = PersonFake.DefaultPersonAge;

        await collection.ReplaceOneAsync(Builders<Person>.Filter.Where(x => x.Id == PersonFake.DefaultPersonId), person);
    }

    public override async Task DownAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(Builders<Person>.Filter.Where(x => x.Id == PersonFake.DefaultPersonId))).FirstOrDefaultAsync();
        person.Age = 0;

        await collection.ReplaceOneAsync(Builders<Person>.Filter.Where(x => x.Id == PersonFake.DefaultPersonId), person);
    }
}