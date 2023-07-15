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
    public async Task Should_Throw_RepeatedVersionException_When_DuplicateMigrations_Found()
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
        var service = serviceProvider.GetRequiredService<IMigrationDatabaseService<IMongoMultiInstance>>();
        var action = async () => await service.ExecuteAsync();

        await action.Should().ThrowAsync<RepeatedVersionException>()
            .WithMessage("Migrations FoodSeed, PersonSeed has repeated version 3.0.1");
    }
    
    [Test]
    public async Task Should_Throw_WrongSemanticVersionException_When_Migration_With_Invalid_Semantic_Version_Found()
    {
        var database = _client.GetDatabase(Guid.NewGuid().ToString());

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
        var action = async () => await service.ExecuteAsync();

        await action.Should().ThrowAsync<WrongSemanticVersionException>()
            .WithMessage($"Migration FoodPriceSeed with version 4.0.1.0 is in wrong format, the correct format should be x.x.x");
    }
    
    [Test]
    public async Task Should_Throw_WrongVersionException_When_Migration_With_Invalid_Version_Found()
    {
        var database = _client.GetDatabase(Guid.NewGuid().ToString());

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
        var action = async () => await service.ExecuteAsync();

        await action.Should().ThrowAsync<WrongVersionException>()
            .WithMessage($"Migration FoodPriceSeed with version 4.B0.1 has wrong path B0. All parts need to be a number");
    }
    
    [Test]
    public async Task Should_Throw_AppliedVersionException_When_Migration_With_Lower_Or_Equal_Version_Found()
    {
        var database = _client.GetDatabase(Guid.NewGuid().ToString());

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
        var action = async () => await service.ExecuteAsync();

        await action.Should().ThrowAsync<AppliedVersionException>()
            .WithMessage($"You can't apply FoodSeed on version 1.0.3, Your version need to be greater than 0.0.1");
    }
    
    [Test]
    public async Task Should_Throw_A_Exception_When_Try_To_Upgrade_When_Exist_A_Downgrade_Version_Less_Than_Upgrade_Version()
    {
        var database = _client.GetDatabase(Guid.NewGuid().ToString());

        var serviceCollection = new ServiceCollection()
            .AddScoped(_ => _loggerFactory)
            .AddScoped(typeof(ILogger<>), typeof(Logger<>))
            .AddMongoMigration(database, x =>
            {
                x.MigrationAssembly = typeof(SeedV1).Assembly;
                x.Namespace = typeof(SeedV4DowngradeVersionException).Namespace;
            });
        
        var migrationCollection = database.GetCollection<MigrationDocument>(MigrationExtensions.COLLECTION_NAME);
        var migrationDocuments = new List<MigrationDocument>()
        {  
            new()
            {
                Id = Guid.NewGuid(),
                Name = "FoodPriceSeed",
                Version = "4.0.4",
                CreatedDate = DateTime.UtcNow
            }
        };
        await migrationCollection.InsertManyAsync(migrationDocuments);

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IMigrationDatabaseService<IMongoMultiInstance>>();
        var action = async () => await service.ExecuteAsync();

        await action.Should().ThrowAsync<DowngradeVersionException>()
            .WithMessage($"You need first apply a downgrade on FoodPriceSeed version 4.0.4 to before apply a upgrade on FoodSeed version 5.0.0");
    }
}