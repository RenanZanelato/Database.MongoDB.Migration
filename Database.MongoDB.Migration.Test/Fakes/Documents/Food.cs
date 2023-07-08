namespace Database.MongoDB.Migration.Test.Fakes.Documents;

public class Food : BaseDocument
{
    public string Name { get; set; }
    public double Price { get; set; }
}