using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database1;

public class FoodSeed : BaseMigration
{
    public override string Version => "2.0.1";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.InsertManyAsync(FoodFake.CreateFoods());
    }

    public override async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(Builders<Food>.Filter.Empty);
    }
}