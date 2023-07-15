using MongoDB.Driver;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database1;

public class FoodPriceSeed : BaseMigration
{
    public override string Version => "2.0.2";
    public override bool IsUp => true;
    
    public override async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(x => x.Id == FoodFake.DefaultFoodId, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        person.Price = FoodFake.DefaultFoodPrice;

        await collection.ReplaceOneAsync(x => x.Id == FoodFake.DefaultFoodId, person, cancellationToken: cancellationToken);
    }

    public override async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var person = await (await collection.FindAsync(x => x.Id == FoodFake.DefaultFoodId, cancellationToken: cancellationToken))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        person.Price = 0;

        await collection.ReplaceOneAsync(x => x.Id == FoodFake.DefaultFoodId, person, cancellationToken: cancellationToken);
    }
}