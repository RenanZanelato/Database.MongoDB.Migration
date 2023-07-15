using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;
using MongoDB.Driver;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V4.WrongDowngrade;

public class FoodSeed : BaseMigration
{
    public override string Version => "5.0.0";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.InsertManyAsync(FoodFake.CreateFoods(), cancellationToken: cancellationToken);
    }

    public override async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        await collection.DeleteManyAsync(Builders<Food>.Filter.Empty, cancellationToken);
    }
}