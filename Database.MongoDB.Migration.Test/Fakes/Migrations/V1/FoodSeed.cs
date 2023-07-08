using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V1;

public class FoodSeed : BaseMigration
{
    public override string Version => "1.0.3";
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