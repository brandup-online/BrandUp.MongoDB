using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoSearchIndexManagerTest
    {
        readonly IMongoSearchIndexManager manager;

        public FakeMongoSearchIndexManagerTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            manager = db.GetCollection<BsonDocument>("test").SearchIndexes;
        }

        [Fact]
        public void CreateOne_AppearsInListing()
        {
            var name = manager.CreateOne(new CreateSearchIndexModel("default", new BsonDocument("mappings", new BsonDocument("dynamic", true))), TestContext.Current.CancellationToken);

            Assert.Equal("default", name);

            var indexes = manager.List(null!, null!, TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Single(indexes);
            Assert.Equal("default", indexes[0]["name"].AsString);
        }

        [Fact]
        public void CreateMany_ReturnsNames()
        {
            var names = manager.CreateMany([
                new CreateSearchIndexModel("a", new BsonDocument()),
                new CreateSearchIndexModel("b", new BsonDocument())
            ], TestContext.Current.CancellationToken);

            Assert.Equal(["a", "b"], names.ToArray());
        }

        [Fact]
        public void List_ByName_ReturnsMatching()
        {
            manager.CreateOne(new CreateSearchIndexModel("a", new BsonDocument()), TestContext.Current.CancellationToken);
            manager.CreateOne(new CreateSearchIndexModel("b", new BsonDocument()), TestContext.Current.CancellationToken);

            var indexes = manager.List("a", null!, TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Single(indexes);
            Assert.Equal("a", indexes[0]["name"].AsString);
        }

        [Fact]
        public void DropOne_RemovesFromListing()
        {
            manager.CreateOne(new CreateSearchIndexModel("a", new BsonDocument()), TestContext.Current.CancellationToken);

            manager.DropOne("a", TestContext.Current.CancellationToken);

            var indexes = manager.List(null!, null!, TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Empty(indexes);
        }

        [Fact]
        public void Update_Missing_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                manager.Update("missing", new BsonDocument(), TestContext.Current.CancellationToken));
        }
    }
}
