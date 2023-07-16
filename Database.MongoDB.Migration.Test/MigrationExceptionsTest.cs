using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Exceptions;
using Database.MongoDB.Migration.Extensions;
using Database.MongoDB.Migration.Interfaces;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using SeedV1 = Database.MongoDB.Migration.Test.Fakes.Migrations.V1.FoodSeed;
using SeedV3 = Database.MongoDB.Migration.Test.Fakes.Migrations.V3.FoodSeed;
using SeedV4SemanticException = Database.MongoDB.Migration.Test.Fakes.Migrations.V4.WrongSemantic.FoodPriceSeed;
using SeedV4VersionException = Database.MongoDB.Migration.Test.Fakes.Migrations.V4.WrongVersion.FoodPriceSeed;
using SeedV4DowngradeVersionException = Database.MongoDB.Migration.Test.Fakes.Migrations.V4.WrongDowngrade.FoodPriceSeed;

namespace Database.MongoDB.Migration.Test;

[TestFixture]
public class MigrationsExceptionTest
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
    public async Task Should_Log_RepeatedVersionException_When_DuplicateMigrations_Found()
    {
        var databaseName = Guid.NewGuid().ToString();
        var database = _client.GetDatabase(databaseName);

        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
        {
            x.MigrationAssembly = typeof(SeedV1).Assembly;
            x.Namespace = typeof(SeedV3).Namespace;
        });

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IMigrationDatabaseService<IMongoMultiInstance>>();
        await service.ExecuteAsync();

        _loggerMock.ReceivedCalls().Should().NotBeEmpty().And.HaveCount(1);
        var logInput = _loggerMock.ReceivedCalls().First().GetArguments();
        logInput[0].Should().Be(LogLevel.Error);
        logInput[2].ToString().Should().Be($"[{databaseName}] Failed to apply migration");
        var expectedMessage = "Migrations FoodSeed, PersonSeed has repeated version 3.0.1";
        logInput[3].GetType().Should().BeAssignableTo<RepeatedVersionException>();
        logInput[3].As<RepeatedVersionException>().Message.Should().Be(expectedMessage);
    }
    
    [Test]
    public async Task Should_Log_WrongSemanticVersionException_When_Migration_With_Invalid_Semantic_Version_Found()
    {
        var databaseName = Guid.NewGuid().ToString();
        var database = _client.GetDatabase(databaseName);
        
        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
            {
                x.MigrationAssembly = typeof(SeedV1).Assembly;
                x.Namespace = typeof(SeedV4SemanticException).Namespace;
            });
        
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IMigrationDatabaseService<IMongoMultiInstance>>();
        await service.ExecuteAsync();

        _loggerMock.ReceivedCalls().Should().NotBeEmpty().And.HaveCount(1);
        var logInput = _loggerMock.ReceivedCalls().First().GetArguments();
        logInput[0].Should().Be(LogLevel.Error);
        logInput[2].ToString().Should().Be($"[{databaseName}] Failed to apply migration");
        var expectedMessage = "Migration FoodPriceSeed with version 4.0.1.0 is in wrong format, the correct format should be x.x.x";
        logInput[3].GetType().Should().BeAssignableTo<WrongSemanticVersionException>();
        logInput[3].As<WrongSemanticVersionException>().Message.Should().Be(expectedMessage);
    }
    
    [Test]
    public async Task Should_Log_WrongVersionException_When_Migration_With_Invalid_Version_Found()
    {
        var databaseName = Guid.NewGuid().ToString();
        var database = _client.GetDatabase(databaseName);
        
        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
            {
                x.MigrationAssembly = typeof(SeedV1).Assembly;
                x.Namespace = typeof(SeedV4VersionException).Namespace;
            });

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IMigrationDatabaseService<IMongoMultiInstance>>();
        await service.ExecuteAsync();

        _loggerMock.ReceivedCalls().Should().NotBeEmpty().And.HaveCount(1);
        var logInput = _loggerMock.ReceivedCalls().First().GetArguments();
        logInput[0].Should().Be(LogLevel.Error);
        logInput[2].ToString().Should().Be($"[{databaseName}] Failed to apply migration");
        var expectedMessage = "Migration FoodPriceSeed with version 4.B0.1 has wrong path B0. All parts need to be a number";
        logInput[3].GetType().Should().BeAssignableTo<WrongVersionException>();
        logInput[3].As<WrongVersionException>().Message.Should().Be(expectedMessage);
    }
    
    [Test]
    public async Task Should_Log_AppliedVersionException_When_Migration_With_Lower_Or_Equal_Version_Found()
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
                Name = "Example",
                Version = "0.0.1",
                CreatedDate = DateTime.UtcNow
            }
        };
        await migrationCollection.InsertManyAsync(migrationDocuments);

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IMigrationDatabaseService<IMongoMultiInstance>>();
        await service.ExecuteAsync();

        _loggerMock.ReceivedCalls().Should().NotBeEmpty().And.HaveCount(1);
        var logInput = _loggerMock.ReceivedCalls().First().GetArguments();
        logInput[0].Should().Be(LogLevel.Error);
        logInput[2].ToString().Should().Be($"[{databaseName}] Failed to apply migration");
        var expectedMessage = "You can't apply FoodSeed on version 1.0.3, Your version need to be greater than 0.0.1";
        logInput[3].GetType().Should().BeAssignableTo<AppliedVersionException>();
        logInput[3].As<AppliedVersionException>().Message.Should().Be(expectedMessage);
    }
}