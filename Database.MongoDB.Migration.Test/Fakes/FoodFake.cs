using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes;

public static class FoodFake
{
    public const string COLLECTION_NAME = "food_example";
    public static Guid DefaultFoodId = Guid.Parse("c0dfe8ad-670c-4389-84d7-093d216bbe11");
    public const string DefaultFoodName = "DefaultFood";
    public const double DefaultFoodPrice = 10.5;

    public static IEnumerable<Food> CreateFoods()
    {
        for (var i = 0; i < 100; i++)
        {
            yield return CreateFood($"ExampleFoods_{i}");
        }

        yield return CreateFood(DefaultFoodId, DefaultFoodName);
    }

    private static Food CreateFood(string name)
        => CreateFood(Guid.NewGuid(), name);
    
    private static Food CreateFood(Guid id, string name)
        => new()
        {
            Id = id,
            Name = name,
            Price = 0
        };
}