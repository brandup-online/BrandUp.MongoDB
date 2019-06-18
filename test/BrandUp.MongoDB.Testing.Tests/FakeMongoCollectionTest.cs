using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionTest
    {
        [Fact]
        public void CountDocuments()
        {
            var client = new FakeMongoClient(MongoUrl.Create("mongodb://localhost:27017"));
            var db = client.GetDatabase("test");
            var collection = db.GetCollection<Document>("test");

            var count = collection.CountDocuments(it => it.Name != "test");

            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertOne()
        {
            var client = new FakeMongoClient(MongoUrl.Create("mongodb://localhost:27017"));
            var db = client.GetDatabase("test");
            var collection = db.GetCollection<Document>("test");

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            Assert.Equal(1, collection.EstimatedDocumentCount());
        }

        [Fact]
        public void ReplaceOne()
        {
            var client = new FakeMongoClient(MongoUrl.Create("mongodb://localhost:27017"));
            var db = client.GetDatabase("test");
            var collection = db.GetCollection<Document>("test");
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc);

            var result = collection.ReplaceOne(it => it.Id == doc.Id, new Document { Id = doc.Id, Name = "test2" });

            Assert.Equal(1, result.MatchedCount);
            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(1, collection.EstimatedDocumentCount());
        }

        [Fact]
        public void FindSync_empty()
        {
            var client = new FakeMongoClient(MongoUrl.Create("mongodb://localhost:27017"));
            var db = client.GetDatabase("test");
            var collection = db.GetCollection<Document>("test");

            var result = collection.FindSync(it => it.Name == "test").ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void FindSync_not_empty()
        {
            var client = new FakeMongoClient(MongoUrl.Create("mongodb://localhost:27017"));
            var db = client.GetDatabase("test");
            var collection = db.GetCollection<Document>("test");
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            var result = collection.FindSync(it => it.Name == "test").ToList();

            Assert.Single(result);
        }
    }

    public class Document
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}