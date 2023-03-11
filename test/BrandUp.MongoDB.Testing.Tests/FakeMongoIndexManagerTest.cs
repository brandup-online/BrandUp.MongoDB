using System;
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
            var name = collection.Indexes.CreateOne(new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)));

            var indexes = collection.Indexes.List().ToList();
            Assert.Single(indexes);
            Assert.Equal("index0", name);
        }

        [Fact]
        public void CreateMany()
        {
            var count = collection.Indexes.CreateMany(new CreateIndexModel<Document>[] {
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)),
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Header))
            });

            var indexes = collection.Indexes.List().ToList();
            Assert.Equal(2, indexes.Count);
        }

        [Fact]
        public void DropAll()
        {
            collection.Indexes.CreateMany(new CreateIndexModel<Document>[] {
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)),
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Header))
            });

            collection.Indexes.DropAll();

            var indexes = collection.Indexes.List().ToList();
            Assert.Empty(indexes);
        }

        [Fact]
        public void DropOne()
        {
            var name = collection.Indexes.CreateOne(new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name)));

            collection.Indexes.DropOne(name);

            var indexes = collection.Indexes.List().ToList();
            Assert.Empty(indexes);
        }

        public class Document
        {
            [BsonId]
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Header { get; set; }
        }
    }
}