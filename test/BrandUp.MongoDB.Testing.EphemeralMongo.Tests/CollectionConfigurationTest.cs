using BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests
{
    public class CollectionConfigurationTest
    {
        [Fact]
        public void Capped_FromAttribute_IsCreatedCapped()
        {
            var services = new ServiceCollection();
            services.AddEphemeralMongoDb();
            services.AddMongoDbContext<CappedEventsDbContext>(o => o.DatabaseName = "Test");

            using var scope = services.BuildServiceProvider();
            var dbContext = EphemeralMongoEnvironment.ResolveOrSkip<CappedEventsDbContext>(scope);

            var options = GetCollectionOptions(dbContext.Database, "capped_events");

            Assert.True(options.GetValue("capped", false).ToBoolean());
            Assert.Equal(1000L, options.GetValue("max", BsonNull.Value).ToInt64());
            Assert.True(options.Contains("size"));
        }

        [Fact]
        public void BlockCompressor_FromAttribute_SetsWiredTigerConfigString()
        {
            var services = new ServiceCollection();
            services.AddEphemeralMongoDb();
            services.AddMongoDbContext<CompressedEventsDbContext>(o => o.DatabaseName = "Test");

            using var scope = services.BuildServiceProvider();
            var dbContext = EphemeralMongoEnvironment.ResolveOrSkip<CompressedEventsDbContext>(scope);

            var options = GetCollectionOptions(dbContext.Database, "compressed_events");

            var configString = options
                .GetValue("storageEngine", new BsonDocument()).AsBsonDocument
                .GetValue("wiredTiger", new BsonDocument()).AsBsonDocument
                .GetValue("configString", BsonString.Empty).AsString;

            Assert.Contains("block_compressor=zstd", configString);
        }

        [Fact]
        public async Task Validation_FromInterface_RejectsInvalidDocument()
        {
            var services = new ServiceCollection();
            services.AddEphemeralMongoDb();
            services.AddMongoDbContext<ValidatedPeopleDbContext>(o => o.DatabaseName = "Test");

            using var scope = services.BuildServiceProvider();
            var dbContext = EphemeralMongoEnvironment.ResolveOrSkip<ValidatedPeopleDbContext>(scope);

            var options = GetCollectionOptions(dbContext.Database, "validated_people");
            Assert.True(options.Contains("validator"));

            await dbContext.People.InsertOneAsync(
                new ValidatedPersonDocument { Name = "ok" },
                cancellationToken: TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<MongoWriteException>(() => dbContext.People.InsertOneAsync(
                new ValidatedPersonDocument { Name = null },
                cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Reconcile_AppliesValidationToExistingCollection()
        {
            var services = new ServiceCollection();
            services.AddEphemeralMongoDb();
            services.AddMongoDbContext<ReconcilePeopleDbContext>(o =>
            {
                o.DatabaseName = "Test";
                o.UpdateExistingCollections = true;
            });

            using var scope = services.BuildServiceProvider();

            // Pre-create the collection without any validator, before the context initializes.
            var factory = EphemeralMongoEnvironment.ResolveOrSkip<IMongoDbClientFactory>(scope);
            var database = factory.ResolveClient().GetDatabase("Test");
            database.CreateCollection("reconcile_people", cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(GetCollectionOptions(database, "reconcile_people").Contains("validator"));

            // Resolving the context reconciles the declared validator onto the existing collection.
            var dbContext = scope.GetRequiredService<ReconcilePeopleDbContext>();

            Assert.True(GetCollectionOptions(database, "reconcile_people").Contains("validator"));

            await Assert.ThrowsAsync<MongoWriteException>(() => dbContext.People.InsertOneAsync(
                new ReconcilePersonDocument { Name = null },
                cancellationToken: TestContext.Current.CancellationToken));
        }

        static BsonDocument GetCollectionOptions(IMongoDatabase database, string name)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("name", name);
            var description = database.ListCollections(new ListCollectionsOptions { Filter = filter }).ToList().FirstOrDefault();
            return description != null && description.TryGetValue("options", out var options) && options.IsBsonDocument
                ? options.AsBsonDocument
                : [];
        }
    }
}
