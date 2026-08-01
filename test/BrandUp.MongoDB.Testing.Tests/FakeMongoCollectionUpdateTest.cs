using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionUpdateTest
    {
        readonly IMongoCollection<Document> collection;

        public FakeMongoCollectionUpdateTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");
        }

        Document Single(Guid id)
        {
            return collection.FindSync(it => it.Id == id, cancellationToken: TestContext.Current.CancellationToken).Single(TestContext.Current.CancellationToken);
        }

        [Fact]
        public void Inc()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Counter = 1 };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            collection.UpdateOne(it => it.Id == doc.Id,
                Builders<Document>.Update.Inc(it => it.Counter, 2).Inc(it => it.Total, 5),
                cancellationToken: TestContext.Current.CancellationToken);

            var updated = Single(doc.Id);
            Assert.Equal(3, updated.Counter);
            Assert.Equal(5, updated.Total);
        }

        [Fact]
        public void Unset()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Email = "a@x.io" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            collection.UpdateOne(it => it.Id == doc.Id,
                Builders<Document>.Update.Unset(it => it.Email),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Null(Single(doc.Id).Email);
        }

        [Fact]
        public void AddToSet_DoesNotDuplicate()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Tags = ["red"] };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<Document>.Update.AddToSet(it => it.Tags, "red"), cancellationToken: TestContext.Current.CancellationToken);
            collection.UpdateOne(it => it.Id == doc.Id, Builders<Document>.Update.AddToSet(it => it.Tags, "blue"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(new[] { "red", "blue" }, Single(doc.Id).Tags);
        }

        [Fact]
        public void Max_OnlyMovesForward()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", LastSeen = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc) };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            // Earlier date must not overwrite (the UserFingerprintRepository monotonic-clock pattern).
            collection.UpdateOne(it => it.Id == doc.Id,
                Builders<Document>.Update.Max(it => it.LastSeen, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), Single(doc.Id).LastSeen);

            collection.UpdateOne(it => it.Id == doc.Id,
                Builders<Document>.Update.Max(it => it.LastSeen, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), Single(doc.Id).LastSeen);
        }

        [Fact]
        public void Min()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Counter = 10 };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<Document>.Update.Min(it => it.Counter, 3), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(3, Single(doc.Id).Counter);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<Document>.Update.Min(it => it.Counter, 7), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(3, Single(doc.Id).Counter);
        }

        [Fact]
        public void Push_Pull()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Tags = ["red"] };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<Document>.Update.Push(it => it.Tags, "red"), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(new[] { "red", "red" }, Single(doc.Id).Tags);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<Document>.Update.Pull(it => it.Tags, "red"), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Empty(Single(doc.Id).Tags);
        }

        [Fact]
        public void UpdateOne_Upsert_InsertsWithSetOnInsert()
        {
            var result = collection.UpdateOne(
                Builders<Document>.Filter.Eq(it => it.Name, "created"),
                Builders<Document>.Update.Set(it => it.Counter, 7).SetOnInsert(it => it.Email, "new@x.io"),
                new UpdateOptions { IsUpsert = true },
                TestContext.Current.CancellationToken);

            Assert.Equal(0, result.MatchedCount);
            Assert.NotNull(result.UpsertedId);

            var created = collection.FindSync(it => it.Name == "created", cancellationToken: TestContext.Current.CancellationToken).Single(TestContext.Current.CancellationToken);
            Assert.Equal(7, created.Counter);
            Assert.Equal("new@x.io", created.Email);
            Assert.NotEqual(Guid.Empty, created.Id);
        }

        [Fact]
        public void UpdateOne_Upsert_ExistingDocument_SetOnInsertIgnored()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Email = "old@x.io" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.UpdateOne(
                Builders<Document>.Filter.Eq(it => it.Name, "a"),
                Builders<Document>.Update.Set(it => it.Counter, 1).SetOnInsert(it => it.Email, "new@x.io"),
                new UpdateOptions { IsUpsert = true },
                TestContext.Current.CancellationToken);

            Assert.Equal(1, result.MatchedCount);
            Assert.Null(result.UpsertedId);
            Assert.Equal("old@x.io", Single(doc.Id).Email);
        }

        [Fact]
        public void FindOneAndUpdate_SortAndReturnAfter_ClaimsFirst()
        {
            // The SendingRepository claim pattern: take the oldest matching document atomically.
            collection.InsertMany([
                new Document { Id = Guid.NewGuid(), Name = "c", Counter = 3 },
                new Document { Id = Guid.NewGuid(), Name = "a", Counter = 1 },
                new Document { Id = Guid.NewGuid(), Name = "b", Counter = 2 }
            ], cancellationToken: TestContext.Current.CancellationToken);

            var claimed = collection.FindOneAndUpdate(
                Builders<Document>.Filter.Empty,
                Builders<Document>.Update.Set(it => it.Email, "claimed"),
                new FindOneAndUpdateOptions<Document, Document>
                {
                    Sort = Builders<Document>.Sort.Ascending(it => it.Counter),
                    ReturnDocument = ReturnDocument.After
                },
                TestContext.Current.CancellationToken);

            Assert.Equal("a", claimed.Name);
            Assert.Equal("claimed", claimed.Email);
        }

        [Fact]
        public void FindOneAndUpdate_Upsert_SequenceGenerator()
        {
            // The AppDocumentNumber pattern: {filter: key} + $inc + upsert + ReturnDocument.After.
            var options = new FindOneAndUpdateOptions<Document, Document>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var first = collection.FindOneAndUpdate(
                Builders<Document>.Filter.Eq(it => it.Name, "invoices"),
                Builders<Document>.Update.Inc(it => it.Counter, 1),
                options,
                TestContext.Current.CancellationToken);
            var second = collection.FindOneAndUpdate(
                Builders<Document>.Filter.Eq(it => it.Name, "invoices"),
                Builders<Document>.Update.Inc(it => it.Counter, 1),
                options,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, first.Counter);
            Assert.Equal(2, second.Counter);
            Assert.Equal(1, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void UpdateOne_NestedPath()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "a", Profile = new Profile { City = "NY" } };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            collection.UpdateOne(it => it.Id == doc.Id,
                Builders<Document>.Update.Set(it => it.Profile!.City, "LA"),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("LA", Single(doc.Id).Profile!.City);
        }

        [Fact]
        public void ReplaceOne_Upsert_Inserts()
        {
            var result = collection.ReplaceOne(
                Builders<Document>.Filter.Eq(it => it.Name, "missing"),
                new Document { Id = Guid.NewGuid(), Name = "missing" },
                new ReplaceOptions { IsUpsert = true },
                TestContext.Current.CancellationToken);

            Assert.Equal(0, result.MatchedCount);
            Assert.NotNull(result.UpsertedId);
            Assert.Equal(1, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ArrayFilters_NotSupported()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);

            var arrayFilters = new[] { new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("x", 1)) };

            Assert.Throws<NotSupportedException>(() =>
                collection.UpdateOne(it => it.Name == "a",
                    Builders<Document>.Update.Set(it => it.Counter, 1),
                    new UpdateOptions { ArrayFilters = arrayFilters },
                    TestContext.Current.CancellationToken));

            Assert.Throws<NotSupportedException>(() =>
                collection.FindOneAndUpdate(it => it.Name == "a",
                    Builders<Document>.Update.Set(it => it.Counter, 1),
                    new FindOneAndUpdateOptions<Document, Document> { ArrayFilters = arrayFilters },
                    TestContext.Current.CancellationToken));
        }

        [Fact]
        public void BulkWrite_UpdateOneModel_Sort_ClaimsSortedDocument()
        {
            collection.InsertMany([
                new Document { Id = Guid.NewGuid(), Name = "high", Counter = 10 },
                new Document { Id = Guid.NewGuid(), Name = "low", Counter = 1 }
            ], cancellationToken: TestContext.Current.CancellationToken);

            collection.BulkWrite([
                new UpdateOneModel<Document>(
                    Builders<Document>.Filter.Empty,
                    Builders<Document>.Update.Set(it => it.Email, "claimed"))
                {
                    Sort = Builders<Document>.Sort.Ascending(it => it.Counter)
                }
            ], cancellationToken: TestContext.Current.CancellationToken);

            var claimed = collection.FindSync(it => it.Email == "claimed", cancellationToken: TestContext.Current.CancellationToken).Single(TestContext.Current.CancellationToken);
            Assert.Equal("low", claimed.Name);
        }

        [Fact]
        public void BulkWrite_UpsertsReported()
        {
            // The ChannelMessageRepository pattern: unordered upserts, then read result.Upserts indices.
            var existing = new Document { Id = Guid.NewGuid(), Name = "exists" };
            collection.InsertOne(existing, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.BulkWrite([
                new UpdateOneModel<Document>(
                    Builders<Document>.Filter.Eq(it => it.Name, "exists"),
                    Builders<Document>.Update.Set(it => it.Counter, 1)) { IsUpsert = true },
                new UpdateOneModel<Document>(
                    Builders<Document>.Filter.Eq(it => it.Name, "new"),
                    Builders<Document>.Update.Set(it => it.Counter, 2)) { IsUpsert = true }
            ], new BulkWriteOptions { IsOrdered = false }, TestContext.Current.CancellationToken);

            Assert.Equal(1, result.MatchedCount);
            var upsert = Assert.Single(result.Upserts);
            Assert.Equal(1, upsert.Index);
            Assert.Equal(2, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        public class Document
        {
            [BsonId]
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
            public int Counter { get; set; }
            public long Total { get; set; }
            public List<string> Tags { get; set; } = [];
            [BsonIgnoreIfNull]
            public string? Email { get; set; }
            public DateTime LastSeen { get; set; }
            [BsonIgnoreIfNull]
            public Profile? Profile { get; set; }
        }

        public class Profile
        {
            public string City { get; set; } = null!;
        }
    }
}
