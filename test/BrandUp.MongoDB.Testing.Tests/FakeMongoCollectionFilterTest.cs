using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionFilterTest
    {
        readonly IMongoCollection<Document> collection;

        public FakeMongoCollectionFilterTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");

            collection.InsertMany([
                new Document { Id = Guid.NewGuid(), Name = "alpha", Counter = 1, Limit = 5, Tags = ["red", "green"] },
                new Document { Id = Guid.NewGuid(), Name = "beta", Counter = 5, Limit = 3, Tags = ["blue"] },
                new Document { Id = Guid.NewGuid(), Name = "gamma", Counter = 10, Limit = 10, Tags = [] }
            ]);
        }

        List<Document> Find(FilterDefinition<Document> filter)
        {
            return collection.FindSync(filter, cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
        }

        [Fact]
        public void Eq()
        {
            var result = Find(Builders<Document>.Filter.Eq(it => it.Name, "alpha"));

            Assert.Single(result);
            Assert.Equal("alpha", result[0].Name);
        }

        [Fact]
        public void Ne()
        {
            var result = Find(Builders<Document>.Filter.Ne(it => it.Name, "alpha"));

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Comparisons()
        {
            Assert.Equal(2, Find(Builders<Document>.Filter.Gt(it => it.Counter, 1)).Count);
            Assert.Equal(3, Find(Builders<Document>.Filter.Gte(it => it.Counter, 1)).Count);
            Assert.Single(Find(Builders<Document>.Filter.Lt(it => it.Counter, 5)));
            Assert.Equal(2, Find(Builders<Document>.Filter.Lte(it => it.Counter, 5)).Count);
        }

        [Fact]
        public void In_Nin()
        {
            Assert.Equal(2, Find(Builders<Document>.Filter.In(it => it.Name, ["alpha", "beta"])).Count);
            Assert.Single(Find(Builders<Document>.Filter.Nin(it => it.Name, ["alpha", "beta"])));
        }

        [Fact]
        public void And_Or_Not()
        {
            var and = Find(Builders<Document>.Filter.And(
                Builders<Document>.Filter.Gt(it => it.Counter, 1),
                Builders<Document>.Filter.Lt(it => it.Counter, 10)));
            Assert.Single(and);
            Assert.Equal("beta", and[0].Name);

            var or = Find(Builders<Document>.Filter.Or(
                Builders<Document>.Filter.Eq(it => it.Name, "alpha"),
                Builders<Document>.Filter.Eq(it => it.Name, "gamma")));
            Assert.Equal(2, or.Count);

            var not = Find(Builders<Document>.Filter.Not(Builders<Document>.Filter.Eq(it => it.Name, "alpha")));
            Assert.Equal(2, not.Count);
        }

        [Fact]
        public void Exists()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "delta", Counter = 0, Limit = 0, Tags = [], Email = "d@x.io" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Single(Find(Builders<Document>.Filter.Exists(it => it.Email)));
            Assert.Equal(3, Find(Builders<Document>.Filter.Exists(it => it.Email, false)).Count);
        }

        [Fact]
        public void Type()
        {
            Assert.Equal(3, Find(Builders<Document>.Filter.Type(it => it.Name, BsonType.String)).Count);
            Assert.Empty(Find(Builders<Document>.Filter.Type(it => it.Name, BsonType.Int32)));
        }

        [Fact]
        public void Regex()
        {
            Assert.Single(Find(Builders<Document>.Filter.Regex(it => it.Name, new BsonRegularExpression("^al"))));
            Assert.Single(Find(Builders<Document>.Filter.Regex(it => it.Name, new BsonRegularExpression("^AL", "i"))));
        }

        [Fact]
        public void AnyEq()
        {
            var result = Find(Builders<Document>.Filter.AnyEq(it => it.Tags, "red"));

            Assert.Single(result);
            Assert.Equal("alpha", result[0].Name);
        }

        [Fact]
        public void Size()
        {
            Assert.Single(Find(Builders<Document>.Filter.Size(it => it.Tags, 2)));
        }

        [Fact]
        public void Expr_ComparesTwoFields()
        {
            // The pattern used by WSender BalanceRepository: {$expr: {$lt: ["$counter", "$limit"]}}.
            var filter = new BsonDocumentFilterDefinition<Document>(
                new BsonDocument("$expr", new BsonDocument("$lt", new BsonArray { "$Counter", "$Limit" })));

            var result = Find(filter);

            Assert.Single(result);
            Assert.Equal("alpha", result[0].Name);
        }

        [Fact]
        public void Null_MatchesMissingField()
        {
            Assert.Equal(3, Find(Builders<Document>.Filter.Eq(it => it.Email, null)).Count);
        }

        [Fact]
        public void Find_Sort_Skip_Limit()
        {
            var result = collection.FindSync(
                Builders<Document>.Filter.Empty,
                new FindOptions<Document, Document>
                {
                    Sort = Builders<Document>.Sort.Descending(it => it.Counter),
                    Skip = 1,
                    Limit = 1
                },
                TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Single(result);
            Assert.Equal("beta", result[0].Name);
        }

        public class Document
        {
            [BsonId]
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
            public int Counter { get; set; }
            public int Limit { get; set; }
            public List<string> Tags { get; set; } = [];
            [BsonIgnoreIfNull]
            public string? Email { get; set; }
        }
    }
}
