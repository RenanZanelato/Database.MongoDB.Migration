using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V4;

public class FoodSeed : BaseMigration
{
    public override int Version => 2;
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.InsertManyAsync(FoodFake.CreateFoods());
    }

    public override async Task DownAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(Builders<Food>.Filter.Empty);
    }
}