using Database.MongoDB.Migration.Test.Fakes.Documents;

namespace Database.MongoDB.Migration.Test.Fakes;

public static class PersonFake
{
    public const string COLLECTION_NAME = "person_example";
    public static Guid DefaultPersonId = Guid.Parse("c0dfe8ad-670c-4389-84d7-093d216bbe11");
    public const string DefaultPersonName = "DefaultName";
    public const int DefaultPersonAge = 25;

    public static IEnumerable<Person> CreatePersons()
    {
        for (var i = 0; i < 100; i++)
        {
            yield return CreatePerson($"ExamplePerson_{i}");
        }

        yield return CreatePerson(DefaultPersonId, DefaultPersonName);
    }

    private static Person CreatePerson(string name)
        => CreatePerson(Guid.NewGuid(), name);
    
    private static Person CreatePerson(Guid id, string name)
        => new Person()
        {
            Id = id,
            Name = name,
            Age = 0
        };
}