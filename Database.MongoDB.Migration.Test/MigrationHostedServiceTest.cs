using Database.MongoDB.Migration.Document;
using Database.MongoDB.Migration.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;

namespace Database.MongoDB.Migration.Test;

[TestFixture]
public class MigrationHostedServiceTest
{
    private ServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var databaseName = Guid.NewGuid().ToString();
        var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
        
        var serviceCollection = new ServiceCollection()
            .AddMongoMigration(client.GetDatabase(databaseName))
            .AddScoped(_ => Substitute.For<IMigrationDatabaseService<IMongoMultiInstance>>());

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider.Dispose();
    }
    
    [Test]
    public async Task Should_Ensure_That_MigrationDocument_Index_Was_Created()
    {
        using var scoped = _serviceProvider.CreateScope();

        var service = scoped.ServiceProvider.GetRequiredService<IHostedService>();
        await service.StartAsync(CancellationToken.None);

        Assert.That(async () =>
        {
            var database = scoped.ServiceProvider.GetRequiredService<IMongoMigrationDatabase<IMongoMultiInstance>>();
            var migrationCollection = database.GetDatabase().GetCollection<MigrationDocument>("_migrations");
            var migrations = await (await migrationCollection.Indexes.ListAsync()).ToListAsync();
            migrations.Should().HaveCount(2);
            return true;
        }, Is.True.After(3).Seconds.PollEvery(1).Seconds);
    }
    
    [Test]
    public async Task Should_Throw_A_Exception_When_Try_To_Create_A_Document_With_The_Same_Version()
    {
        using var scoped = _serviceProvider.CreateScope();

        var service = scoped.ServiceProvider.GetRequiredService<IHostedService>();
        await service.StartAsync(CancellationToken.None);

        Assert.That(async () =>
        {
            var fakeDocument = new List<MigrationDocument>()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "example1",
                    Version = "1.0.0",
                    CreatedDate = DateTime.UtcNow,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "example2",
                    Version = "1.0.0",
                    CreatedDate = DateTime.UtcNow,
                }
            };
            var database = scoped.ServiceProvider.GetRequiredService<IMongoMigrationDatabase<IMongoMultiInstance>>();
            var migrationCollection = database.GetDatabase().GetCollection<MigrationDocument>("_migrations");

            var func = async () => await migrationCollection.InsertManyAsync(fakeDocument);
            await func.Should().ThrowAsync<MongoBulkWriteException>();

            return true;
        }, Is.True.After(3).Seconds.PollEvery(1).Seconds);
    }
}