using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V4.WrongDowngrade;

public class FoodPriceSeed : BaseMigration
{
    public override string Version => "4.0.4";
    public override bool IsUp => false;
    
    public override async Task UpAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId))).FirstOrDefaultAsync();
        person.Price = 0;

        await collection.ReplaceOneAsync(Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId), person);
    }

    public override async Task DownAsync(IMongoDatabase database)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId))).FirstOrDefaultAsync();
        person.Price = FoodFake.DefaultFoodPrice;

        await collection.ReplaceOneAsync(Builders<Food>.Filter.Where(x => x.Id == FoodFake.DefaultFoodId), person);
    }
}