using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoDatabaseTest
    {
        readonly IMongoDatabase database;

        public FakeMongoDatabaseTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            database = client.GetDatabase("test");
        }

        [Fact]
        public void GetCollection_SameNameReturnsSameInstance()
        {
            var c1 = database.GetCollection<Document>("docs");
            var c2 = database.GetCollection<Document>("docs");

            Assert.Same(c1, c2);
        }

        [Fact]
        public void CreateCollection_MakesCollectionAppearInListing()
        {
            database.CreateCollection("created", cancellationToken: TestContext.Current.CancellationToken);

            var names = database.ListCollectionNames(cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);

            Assert.Contains("created", names);
        }

        [Fact]
        public void CreateCollection_Twice_Throws()
        {
            database.CreateCollection("dup", cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<InvalidOperationException>(() =>
                database.CreateCollection("dup", cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void CreateCollection_ThenGetCollection_DoesNotDuplicateAndIsTyped()
        {
            database.CreateCollection("typed", cancellationToken: TestContext.Current.CancellationToken);

            var collection = database.GetCollection<Document>("typed");
            collection.InsertOne(new Document { Name = "x" }, cancellationToken: TestContext.Current.CancellationToken);

            var names = database.ListCollectionNames(cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);

            Assert.Single(names, n => n == "typed");
            Assert.Equal(1, collection.CountDocuments(FilterDefinition<Document>.Empty, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void CreateCollection_ThenDrop_RemovesFromListing()
        {
            database.CreateCollection("temp", cancellationToken: TestContext.Current.CancellationToken);
            database.DropCollection("temp", cancellationToken: TestContext.Current.CancellationToken);

            var names = database.ListCollectionNames(cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);

            Assert.DoesNotContain("temp", names);
        }

        [Fact]
        public async Task ListCollections_ReturnsCreatedCollections()
        {
            database.GetCollection<Document>("alpha");
            database.GetCollection<Document>("beta");

            var docs = await (await database.ListCollectionsAsync(cancellationToken: TestContext.Current.CancellationToken))
                .ToListAsync(TestContext.Current.CancellationToken);

            var names = docs.Select(d => d["name"].AsString).OrderBy(n => n).ToArray();
            Assert.Equal(["alpha", "beta"], names);
            Assert.All(docs, d => Assert.Equal("collection", d["type"].AsString));
        }

        [Fact]
        public async Task DropCollection_RemovesFromListing()
        {
            database.GetCollection<Document>("alpha");
            database.GetCollection<Document>("beta");

            await database.DropCollectionAsync("alpha", TestContext.Current.CancellationToken);

            var names = await (await database.ListCollectionNamesAsync(cancellationToken: TestContext.Current.CancellationToken))
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.DoesNotContain("alpha", names);
            Assert.Contains("beta", names);
        }

        [Fact]
        public void RenameCollection_MovesEntryAndPreservesData()
        {
            var source = database.GetCollection<Document>("source");
            source.InsertOne(new Document { Name = "x" }, cancellationToken: TestContext.Current.CancellationToken);

            database.RenameCollection("source", "target", cancellationToken: TestContext.Current.CancellationToken);

            var target = database.GetCollection<Document>("target");
            Assert.Equal(1, target.CountDocuments(FilterDefinition<Document>.Empty, cancellationToken: TestContext.Current.CancellationToken));

            var names = database.ListCollectionNames(cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("source", names);
        }

        [Fact]
        public void RenameCollection_MissingSource_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                database.RenameCollection("missing", "target", cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void RenameCollection_ExistingTargetWithoutDropTarget_Throws()
        {
            database.GetCollection<Document>("source");
            database.GetCollection<Document>("target");

            Assert.Throws<InvalidOperationException>(() =>
                database.RenameCollection("source", "target", cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void With_ReturnsSameInstance()
        {
            Assert.Same(database, database.WithReadConcern(ReadConcern.Majority));
            Assert.Same(database, database.WithReadPreference(ReadPreference.Primary));
            Assert.Same(database, database.WithWriteConcern(WriteConcern.WMajority));
        }

        [Fact]
        public void RunCommand_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() =>
                database.RunCommand<BsonDocument>("{ ping: 1 }", cancellationToken: TestContext.Current.CancellationToken));
        }

        public class Document
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public string Name { get; set; } = null!;
        }
    }
}
