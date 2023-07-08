namespace Database.MongoDB.Migration.Test.Fakes.Documents;

public class Person: BaseDocument
{
    public string Name { get; set; }
    public int Age { get; set; }
}