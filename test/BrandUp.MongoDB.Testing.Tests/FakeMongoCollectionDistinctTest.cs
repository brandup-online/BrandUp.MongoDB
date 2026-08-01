using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionDistinctTest
    {
        readonly IMongoCollection<Document> collection;

        public FakeMongoCollectionDistinctTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");

            collection.InsertMany([
                new Document { Id = Guid.NewGuid(), Type = "a", Counter = 1, Tags = ["x", "y"] },
                new Document { Id = Guid.NewGuid(), Type = "b", Counter = 2, Tags = ["y", "z"] },
                new Document { Id = Guid.NewGuid(), Type = "a", Counter = 3, Tags = [] }
            ]);
        }

        [Fact]
        public void Distinct_ReturnsUniqueValues()
        {
            var values = collection.Distinct(it => it.Type, Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);

            Assert.Equal(["a", "b"], values);
        }

        [Fact]
        public void Distinct_WithFilter()
        {
            var values = collection.Distinct(it => it.Type, Builders<Document>.Filter.Gte(it => it.Counter, 2), cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);

            Assert.Equal(["b", "a"], values);
        }

        [Fact]
        public void Distinct_ArrayField_FlattensValues()
        {
            var values = collection.Distinct<string>("Tags", Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken)
                .ToList(TestContext.Current.CancellationToken);

            Assert.Equal(["x", "y", "z"], values);
        }

        [Fact]
        public void CountDocuments_RenderedFilter()
        {
            var count = collection.CountDocuments(Builders<Document>.Filter.Gte(it => it.Counter, 2), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void CountDocuments_SkipLimit()
        {
            var count = collection.CountDocuments(Builders<Document>.Filter.Empty, new CountOptions { Skip = 1, Limit = 1 }, TestContext.Current.CancellationToken);

            Assert.Equal(1, count);
        }

        public class Document
        {
            [BsonId]
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
            public string Type { get; set; } = null!;
            public int Counter { get; set; }
            public List<string> Tags { get; set; } = [];
        }
    }
}
