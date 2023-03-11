using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionTest
    {
        readonly IMongoCollection<Document> collection;

        public FakeMongoCollectionTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");
        }

        [Fact]
        public void CountDocuments()
        {
            var count = collection.CountDocuments(it => it.Name != "test");

            Assert.Equal(0, count);
        }

        [Fact]
        public void CountDocuments_Empty()
        {
            var count = collection.CountDocuments(Builders<Document>.Filter.Empty);

            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertOne()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            Assert.Equal(1, collection.EstimatedDocumentCount());
        }

        [Fact]
        public void UpdateOne()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc);

            var updateResult = collection.UpdateOne(it => it.Name == "test", Builders<Document>.Update.Set(it => it.Name, "test2"));

            Assert.Equal(1, updateResult.MatchedCount);
            Assert.Equal(1, updateResult.ModifiedCount);

            var updatedDoc = collection.Find(it => it.Name == "test2").FirstOrDefault();
            Assert.NotNull(updatedDoc);
            Assert.Equal(doc.Id, updatedDoc.Id);
            Assert.Equal("test2", updatedDoc.Name);
        }

        [Fact]
        public void UpdateMany()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            var updateResult = collection.UpdateMany(it => it.Name == "test", Builders<Document>.Update.Set(it => it.Name, "test2"));

            Assert.Equal(2, updateResult.MatchedCount);
            Assert.Equal(2, updateResult.ModifiedCount);

            foreach (var updatedDoc in collection.Find(it => it.Name == "test2").ToList())
                Assert.Equal("test2", updatedDoc.Name);
        }

        [Fact]
        public void ReplaceOne()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc);

            var result = collection.ReplaceOne(it => it.Id == doc.Id, new Document { Id = doc.Id, Name = "test2" });

            Assert.Equal(1, result.MatchedCount);
            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(1, collection.EstimatedDocumentCount());

            var replacedDoc = collection.Find(it => it.Name == "test2").FirstOrDefault();
            Assert.NotNull(replacedDoc);
            Assert.Equal(doc.Id, replacedDoc.Id);
            Assert.Equal("test2", replacedDoc.Name);
        }

        [Fact]
        public void DeleteOne()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc);

            var result = collection.DeleteOne(it => it.Id == doc.Id);

            Assert.Equal(1, result.DeletedCount);
            Assert.Equal(0, collection.EstimatedDocumentCount());
        }

        [Fact]
        public void DeleteMany()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            var result = collection.DeleteMany(it => it.Name == "test");

            Assert.Equal(2, result.DeletedCount);
            Assert.Equal(0, collection.EstimatedDocumentCount());
        }

        [Fact]
        public void FindSync_not_matched()
        {
            var result = collection.FindSync(it => it.Name == "test").ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void FindSync_matched()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            var result = collection.FindSync(it => it.Name == "test").ToList();

            Assert.Single(result);
        }

        [Fact]
        public void FindSync_empty_filter()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" });

            var result = collection.FindSync(Builders<Document>.Filter.Empty).ToList();

            Assert.Single(result);
        }

        public class Document
        {
            [BsonId]
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}