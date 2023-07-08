using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V1;

public class FoodSeed : BaseMigration
{
    public override int Version => 3;
    public override bool IsUp => true;
    
    public override async Task UpAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.InsertManyAsync(clientSessionHandle, FoodFake.CreateFoods());
    }

    public override async Task DownAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(clientSessionHandle, Builders<Food>.Filter.Empty);
    }
}