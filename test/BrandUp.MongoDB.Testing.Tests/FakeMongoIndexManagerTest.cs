using System;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoIndexManagerTest
    {
        readonly IMongoCollection<Document> collection;

        public FakeMongoIndexManagerTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");
        }

        [Fact]
        public void CreateOne()
        {
            var name = collection.Indexes.CreateOne(new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)), cancellationToken: TestContext.Current.CancellationToken);

            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            // The implicit _id index is always reported, like on a real server.
            Assert.Equal(2, indexes.Count);
            // Default name follows the server convention.
            Assert.Equal("Name_1", name);
        }

        [Fact]
        public void CreateOne_UnnamedSameKeysTwice_IsNoOp()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)), cancellationToken: TestContext.Current.CancellationToken);
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)), cancellationToken: TestContext.Current.CancellationToken);

            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Single(indexes, it => it["name"] == "Name_1");
            Assert.Equal(2, indexes.Count);
        }

        [Fact]
        public void List_AlwaysContainsIdIndex()
        {
            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Single(indexes, it => it["name"] == "_id_");
        }

        [Fact]
        public void DropOne_IdIndex_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                collection.Indexes.DropOne("_id_", TestContext.Current.CancellationToken));
        }

        [Fact]
        public void CreateMany()
        {
            var count = collection.Indexes.CreateMany([
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)),
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Header))
            ], TestContext.Current.CancellationToken);

            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Equal(3, indexes.Count);
        }

        [Fact]
        public void DropAll()
        {
            collection.Indexes.CreateMany([
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)),
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Header))
            ], TestContext.Current.CancellationToken);

            collection.Indexes.DropAll(TestContext.Current.CancellationToken);

            // DropAll keeps the implicit _id index, like on a real server.
            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Single(indexes, it => it["name"] == "_id_");
            Assert.Single(indexes);
        }

        [Fact]
        public void DropOne()
        {
            var name = collection.Indexes.CreateOne(new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)), cancellationToken: TestContext.Current.CancellationToken);

            collection.Indexes.DropOne(name, TestContext.Current.CancellationToken);

            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Single(indexes, it => it["name"] == "_id_");
        }

        [Fact]
        public void DropOne_MixedCaseName_RemovesIndex()
        {
            collection.Indexes.CreateOne(
                new CreateIndexModel<Document>(
                    Builders<Document>.IndexKeys.Ascending(it => it.Name),
                    new CreateIndexOptions { Name = "MyIndex" }),
                cancellationToken: TestContext.Current.CancellationToken);

            collection.Indexes.DropOne("MyIndex", TestContext.Current.CancellationToken);

            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Single(indexes, it => it["name"] == "_id_");
        }

        [Fact]
        public void DropOne_Missing_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                collection.Indexes.DropOne("missing", TestContext.Current.CancellationToken));
        }

        [Fact]
        public void CreateOne_SameNameAndKeys_IsNoOp()
        {
            var model = new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Name = "name_idx" });

            collection.Indexes.CreateOne(model, cancellationToken: TestContext.Current.CancellationToken);
            collection.Indexes.CreateOne(model, cancellationToken: TestContext.Current.CancellationToken);

            var indexes = collection.Indexes.List(TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            Assert.Single(indexes, it => it["name"] == "name_idx");
        }

        [Fact]
        public void CreateOne_SameNameDifferentKeys_Throws()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Name = "idx" }), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<InvalidOperationException>(() =>
                collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                    Builders<Document>.IndexKeys.Ascending(it => it.Header),
                    new CreateIndexOptions { Name = "idx" }), cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateManyAsync_WithSession_DoesNotRecurse()
        {
            using var session = collection.Database.Client.StartSession(cancellationToken: TestContext.Current.CancellationToken);

            var names = await collection.Indexes.CreateManyAsync(session, [
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)),
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Header))
            ], TestContext.Current.CancellationToken);

            Assert.Equal(2, names.Count());
        }

        public class Document
        {
            [BsonId]
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
            public string Header { get; set; } = null!;
        }
    }
}