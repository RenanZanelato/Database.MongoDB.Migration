using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Exceptions;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Database.MongoDB.Migration.Migration;
using Database.MongoDB.Migration.Test.Fakes;
using Database.MongoDB.Migration.Test.Fakes.Documents;
using Database.MongoDB.Migration.Test.Fakes.Migrations;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using SeedV1 = Database.MongoDB.Migration.Test.Fakes.Migrations.V1.FoodSeed;
using SeedDatabase1 = Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database1.FoodSeed;
using SeedDatabase2 = Database.MongoDB.Migration.Test.Fakes.Migrations.V2.Database2.PersonSeed;
using SeedV3 = Database.MongoDB.Migration.Test.Fakes.Migrations.V3.FoodSeed;

namespace Database.MongoDB.Migration.Test;

[TestFixture]
public class MigrationsTest
{
    private ILogger _loggerMock;
    private ILoggerFactory _loggerFactory;
    private MongoClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _loggerMock = Substitute.For<ILogger>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_loggerMock);
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json")
            .Build();
        
        _client = new MongoClient(configuration.GetConnectionString("MongoDB"));
    }
    
    [TearDown]
    public void TearDown()
    {
        _loggerMock.ClearReceivedCalls();
    }
    
    [Test]
    public async Task Should_Ensure_That_MigrationDocument_Index_Was_Created()
    {
        var database = _client.GetDatabase(Guid.NewGuid().ToString());

        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database);

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(CancellationToken.None);

        Assert.That(async () =>
        {
            var migrationCollection = database.GetCollection<BsonDocument>("_migrations");
            var migrations = await (await migrationCollection.Indexes.ListAsync()).ToListAsync();
            migrations.Should().HaveCount(2);
            return true;
        }, Is.True.After(3).Seconds.PollEvery(1).Seconds);
    }
    
    [Test]
    public async Task Should_Execute_All_V1_Migrations_For_SingleDatabase()
    {
        var databaseName = Guid.NewGuid().ToString();
        var database = _client.GetDatabase(databaseName);

        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
        {
            x.MigrationAssembly = typeof(SeedV1).Assembly;
            x.Namespace = typeof(SeedV1).Namespace;
        });

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetService<IMigrationDatabaseRunner<MongoDefaultInstance>>();
        await service.RunMigrationsAsync();

        var personCollection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var persons = await (await personCollection.FindAsync(Builders<Person>.Filter.Empty)).ToListAsync();
        var defaultPerson = persons.FirstOrDefault(x => x.Id == PersonFake.DefaultPersonId);

        var foodCollection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var foods = await (await foodCollection.FindAsync(Builders<Food>.Filter.Empty)).ToListAsync();
        var defaultFood = foods.FirstOrDefault(x => x.Id == FoodFake.DefaultFoodId);

        var migrationCollection = database.GetCollection<BsonDocument>("_migrations");
        var migrations = await (await migrationCollection.FindAsync(Builders<BsonDocument>.Filter.Empty)).ToListAsync();

        persons.Should().HaveCount(101);
        defaultPerson.Age.Should().Be(PersonFake.DefaultPersonAge);
        
        foods.Should().HaveCount(101);
        defaultFood.Price.Should().Be(0);

        migrations.Should().HaveCount(3);
        _loggerMock.Received(1).LogInformation($"[{databaseName}][PersonSeed][1] Up Successfully");
        _loggerMock.Received(1).LogInformation($"[{databaseName}][PersonAgeSeed][2] Up Successfully");
        _loggerMock.Received(1).LogInformation($"[{databaseName}][FoodSeed][3] Up Successfully");
    }
    
    [Test]
    public async Task Should_Execute_All_V1_Migrations_For_SingleDatabase_And_Execute_One_Downgrade()
    {
        var databaseName = Guid.NewGuid().ToString();
        var database = _client.GetDatabase(databaseName);
        
        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
        {
            x.MigrationAssembly = typeof(SeedV1).Assembly;
            x.Namespace = typeof(SeedV1).Namespace;
        });
        
        var migrationCollection = database.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        var migrationDocument = new MigrationDocument()
        {
            Id = Guid.NewGuid(),
            Name = "FoodPriceSeed",
            Version = 4,
            CreatedDate = DateTime.UtcNow
        };
        await migrationCollection.InsertOneAsync(migrationDocument);
        
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetService<IMigrationDatabaseRunner<MongoDefaultInstance>>();
        await service.RunMigrationsAsync();

        var personCollection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var persons = await (await personCollection.FindAsync(Builders<Person>.Filter.Empty)).ToListAsync();
        var defaultPerson = persons.FirstOrDefault(x => x.Id == PersonFake.DefaultPersonId);

        var foodCollection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var foods = await (await foodCollection.FindAsync(Builders<Food>.Filter.Empty)).ToListAsync();
        var defaultFood = foods.FirstOrDefault(x => x.Id == FoodFake.DefaultFoodId);

        persons.Should().HaveCount(101);
        defaultPerson.Age.Should().Be(PersonFake.DefaultPersonAge);
        
        foods.Should().HaveCount(101);
        defaultFood.Price.Should().Be(FoodFake.DefaultFoodPrice);

        _loggerMock.Received(1).LogInformation($"[{databaseName}][PersonSeed][1] Up Successfully");
        _loggerMock.Received(1).LogInformation($"[{databaseName}][PersonAgeSeed][2] Up Successfully");
        _loggerMock.Received(1).LogInformation($"[{databaseName}][FoodSeed][3] Up Successfully");
    }
    
    [Test]
    public async Task Should_Do_Nothing_When_Migration_Already_Exist()
    {
        var databaseName = Guid.NewGuid().ToString();
        var database = _client.GetDatabase(databaseName);
        
        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
        {
            x.MigrationAssembly = typeof(SeedV1).Assembly;
            x.Namespace = typeof(SeedV1).Namespace;
        });
        
        var migrationCollection = database.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        var migrationDocuments = new List<MigrationDocument>()
        {  
            new()
            {
                Id = Guid.NewGuid(),
                Name = "PersonSeed",
                Version = 1,
                CreatedDate = DateTime.UtcNow
            }, new()
            {
                Id = Guid.NewGuid(),
                Name = "PersonAgeSeed",
                Version = 2,
                CreatedDate = DateTime.UtcNow
            }, new()
            {
                Id = Guid.NewGuid(),
                Name = "FoodSeed",
                Version = 3,
                CreatedDate = DateTime.UtcNow
            }
        };
        await migrationCollection.InsertManyAsync(migrationDocuments);
        
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetService<IMigrationDatabaseRunner<MongoDefaultInstance>>();
        await service.RunMigrationsAsync();

        var personCollection = database.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var persons = await (await personCollection.FindAsync(Builders<Person>.Filter.Empty)).ToListAsync();

        var foodCollection = database.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var foods = await (await foodCollection.FindAsync(Builders<Food>.Filter.Empty)).ToListAsync();

        persons.Should().HaveCount(0);
        foods.Should().HaveCount(0);

        _loggerMock.ReceivedCalls().Should().BeEmpty();
    }
    
    [Test]
    public async Task Should_Execute_All_V2_Migrations_For_MultiDatabase()
    {
        var database1 = _client.GetDatabase(Guid.NewGuid().ToString());
        var database2 = _client.GetDatabase(Guid.NewGuid().ToString());

        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => Substitute.For<ILoggerFactory>())
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration<FoodInstance>(database1, x =>
        {
            x.MigrationAssembly = typeof(SeedV1).Assembly;
            x.Namespace = typeof(SeedDatabase1).Namespace;
        }).AddMongoMigration<PersonInstance>(database2, x =>
            {
                x.MigrationAssembly = typeof(SeedV1).Assembly;
                x.Namespace = typeof(SeedDatabase2).Namespace;
            });;

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var services = serviceProvider.GetServices<IHostedService>();
        foreach (var hostedService in services)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }
        
        Assert.That(async () =>
        {
            var personCollectionDatabase1 = database1.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var personsDatabase1 = await (await personCollectionDatabase1.FindAsync(Builders<Person>.Filter.Empty)).ToListAsync();
        
        var foodCollectionDatabase1 = database1.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var foodsDatabase1 = await (await foodCollectionDatabase1.FindAsync(Builders<Food>.Filter.Empty)).ToListAsync();
        var defaultFood = foodsDatabase1.FirstOrDefault(x => x.Id == FoodFake.DefaultFoodId);
        
        var migrationDatabase1 = database1.GetCollection<BsonDocument>("_migrations");
        var migrationsDatabase1 = await (await migrationDatabase1.FindAsync(Builders<BsonDocument>.Filter.Empty)).ToListAsync();
        
        migrationsDatabase1.Should().HaveCount(2);
        personsDatabase1.Should().HaveCount(0);
        foodsDatabase1.Should().HaveCount(101);
        defaultFood.Price.Should().Be(FoodFake.DefaultFoodPrice);
        
        var personCollectionDatabase2 = database2.GetCollection<Person>(PersonFake.COLLECTION_NAME);
        var personsDatabase2 = await (await personCollectionDatabase2.FindAsync(Builders<Person>.Filter.Empty)).ToListAsync();
        var defaultPerson = personsDatabase2.FirstOrDefault(x => x.Id == PersonFake.DefaultPersonId);

        var foodCollectionDatabase2 = database2.GetCollection<Food>(FoodFake.COLLECTION_NAME);
        var foodsDatabase2 = await (await foodCollectionDatabase2.FindAsync(Builders<Food>.Filter.Empty)).ToListAsync();
        
        var migrationDatabase2 = database2.GetCollection<BsonDocument>("_migrations");
        var migrationsDatabase2 = await (await migrationDatabase2.FindAsync(Builders<BsonDocument>.Filter.Empty)).ToListAsync();
        
        migrationsDatabase2.Should().HaveCount(2);
        personsDatabase2.Should().HaveCount(101);
        defaultPerson.Age.Should().Be(PersonFake.DefaultPersonAge);
        foodsDatabase2.Should().HaveCount(0);
        return true;
        }, Is.True.After(2).Seconds.PollEvery(1).Seconds);
    }
    
    [Test]
    public async Task Should_Throw_A_Exception_When_Exist_Files_With_The_Same_Version()
    {
        var database = _client.GetDatabase(Guid.NewGuid().ToString());

        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
        {
            x.MigrationAssembly = typeof(SeedV1).Assembly;
            x.Namespace = typeof(SeedV3).Namespace;
        });
        
   
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetService<IMigrationDatabaseRunner<MongoDefaultInstance>>();
        var action = async () => await service.RunMigrationsAsync();

        await action.Should().ThrowAsync<VersionException>()
            .WithMessage("The files FoodSeed, PersonSeed has repeated version 1");
    }
}