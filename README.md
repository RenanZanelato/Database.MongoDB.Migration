
# MongoDBMigration
MongoDBMigration is a simple library open-sorce that facilitates MongoDB database migration through a version-based migration system. It provides a convenient way to manage and execute MongoDB database migrations in a controlled and organized manner.

# Features
* Supports multiple MongoDB instances
* Automatically creates migration indexes
* Validates migration versions to avoid duplicates

# Installation
* This implation is based using the official library [MongoDB.Driver](https://github.com/mongodb/mongo-csharp-driver)
* Use NuGet to install the MongoDBMigration library into your project. You can use the following command in the Package Manager Console:
[Nuget](https://www.nuget.org/packages/Database.MongoDB.Migration/1.0.0)
````
Install-Package Database.MongoDB.Migration
````

# Configurations
The MongoDBMigration library is easy to use and requires just a few configurations. Follow the steps below to get started:

Configure your MongoDB database connection:

``` c#
IMongoClient mongoClient = new MongoClient("mongodb://localhost:27017");
IMongoDatabase mongoDatabase = mongoClient.GetDatabase("your-database");
serviceCollection.AddMongoMigration(mongoDatabase);
```

OR, you can use some parameters to configure
``` c#
IMongoClient mongoClient = new MongoClient("mongodb://localhost:27017");
IMongoDatabase mongoDatabase = mongoClient.GetDatabase("your-database");
serviceCollection.AddMongoMigration(mongoDatabase, x =>
        {
            x.MigrationAssembly = typeof(SomeAssemblyWhereWilBeYourMigrations).Assembly;
            x.Namespace = typeof(IfYouWillUseJustASpecifyNamespace).Namespace; //optional
        }));
```

If you Will need a instance using another database, you can do this.
``` c#

IMongoClient mongoClient = new MongoClient("mongodb://localhost:27017");
IMongoDatabase mongoDatabase = mongoClient.GetDatabase("your-database");
IMongoDatabase mongoDatabase2 = mongoClient.GetDatabase("your-another-database");
serviceCollection.AddMongoMigration(mongoDatabase, x =>
        {
            x.MigrationAssembly = typeof(SomeAssemblyWhereWilBeYourMigrations).Assembly;
            x.Namespace = typeof(IfYouWillUseJustASpecifyNamespace).Namespace; //optional
        }.AddMongoMigration<ExampleMultiInstance>(mongoDatabase, x =>
        {
            x.MigrationAssembly = typeof(SomeAssemblyWhereWilBeYourMigrations).Assembly;
            x.Namespace = typeof(AnotherNamespace).Namespace; //optional
        }));

```

this ExampleMultiInstance will need be a class, that implement a IMongoMultiInstance

``` c#
public class ExampleMultiInstance : IMongoMultiInstance
{
    
}
```

And it's done. When your serviceProvider was created, will run a HostedService that will execute your migrations.

# How to create Migration

* Create your migrations by inheriting from the BaseMigration class and implement the Migrate method to perform the database changes:

``` c#
public class ExampleMigration : BaseMigration
{
    public override int Version => 1; //required 
    public override bool IsUp => true; //required, will inform if will need to downgrade your migration 
    //False - Will do a downgrade if the migration was already applyed
    //True - Will apply the migration
    
    public override async Task UpAsync(IMongoDatabase database)
    {
        // Your migration logical here
    }

    public override async Task DownAsync(IMongoDatabase database)
    {
        // Your migration logical here
    }
}
```
Now you are ready to use the MongoDB database migration system provided by MongoDBMigration.

## Contribution
Contributions are welcome! If you encounter an issue, have any ideas, or want to add a new feature, feel free to open an issue or submit a pull request.

## License
This project is licensed under the *MIT License*.

## Contact
If you have any questions, suggestions, or just want to get in touch, you can find me on:

* [Linkedin](https://www.linkedin.com/in/renan-zanelato/)
* [Here on Github](https://github.com/RenanZanelato)

## Buy me a coffee?
* [Paypal](https://www.paypal.com/donate/?business=ZURDAZD3GJX96&no_recurring=0&currency_code=BRL)