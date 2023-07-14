using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database1;

public class FoodPriceSeed : BaseMigration
{
    public override string Version => "2.0.2";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId))).FirstOrDefaultAsync();
        person.Price = FoodFake.DefaultFoodPrice;

        await collection.ReplaceOneAsync(clientSessionHandle, Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId), person);
    }

    public override async Task DownAsync(IClientSessionHandle clientSessionHandle, IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId))).FirstOrDefaultAsync();
        person.Price = 0;

        await collection.ReplaceOneAsync(clientSessionHandle, Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId), person);
    }
}