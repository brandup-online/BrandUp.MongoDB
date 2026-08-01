using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionTransactionTest
    {
        readonly FakeMongoClient client;
        readonly IMongoCollection<Document> collection;

        public FakeMongoCollectionTransactionTest()
        {
            client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");
        }

        long Count()
        {
            return collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken);
        }

        [Fact]
        public void Abort_RollsBackInsert()
        {
            using var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken);
            session.StartTransaction();

            collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, Count());

            session.AbortTransaction(TestContext.Current.CancellationToken);

            Assert.Equal(0, Count());
        }

        [Fact]
        public void Commit_KeepsChanges()
        {
            using var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken);
            session.StartTransaction();

            collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);
            session.CommitTransaction(TestContext.Current.CancellationToken);

            Assert.Equal(1, Count());
        }

        [Fact]
        public void Abort_RollsBackUpdateAndDelete()
        {
            var kept = new Document { Id = Guid.NewGuid(), Name = "kept" };
            var removed = new Document { Id = Guid.NewGuid(), Name = "removed" };
            collection.InsertOne(kept, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(removed, cancellationToken: TestContext.Current.CancellationToken);

            using (var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken))
            {
                session.StartTransaction();

                collection.UpdateOne(session, it => it.Id == kept.Id, Builders<Document>.Update.Set(it => it.Name, "changed"), cancellationToken: TestContext.Current.CancellationToken);
                collection.DeleteOne(session, it => it.Id == removed.Id, cancellationToken: TestContext.Current.CancellationToken);
                Assert.Equal(1, Count());

                session.AbortTransaction(TestContext.Current.CancellationToken);
            }

            Assert.Equal(2, Count());
            var restored = collection.FindSync(it => it.Id == kept.Id, cancellationToken: TestContext.Current.CancellationToken).Single(TestContext.Current.CancellationToken);
            Assert.Equal("kept", restored.Name);
        }

        [Fact]
        public void Dispose_WithoutCommit_RollsBack()
        {
            using (var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken))
            {
                session.StartTransaction();
                collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);
            }

            Assert.Equal(0, Count());
        }

        [Fact]
        public void WritesOutsideTransaction_AreNotRolledBack()
        {
            // The BalanceRepository escape pattern: a deliberate session-less write survives the abort.
            using var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken);
            session.StartTransaction();

            collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "inside" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "outside" }, cancellationToken: TestContext.Current.CancellationToken);

            session.AbortTransaction(TestContext.Current.CancellationToken);

            var remaining = collection.FindSync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            var doc = Assert.Single(remaining);
            Assert.Equal("outside", doc.Name);
        }

        [Fact]
        public void TwoCollections_BothRolledBack()
        {
            var second = client.GetDatabase("test").GetCollection<Document>("other");

            using var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken);
            session.StartTransaction();

            collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);
            second.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "b" }, cancellationToken: TestContext.Current.CancellationToken);

            session.AbortTransaction(TestContext.Current.CancellationToken);

            Assert.Equal(0, Count());
            Assert.Equal(0, second.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void SequentialTransactions_OnSameSession()
        {
            using var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken);

            session.StartTransaction();
            collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "first" }, cancellationToken: TestContext.Current.CancellationToken);
            session.CommitTransaction(TestContext.Current.CancellationToken);

            session.StartTransaction();
            collection.InsertOne(session, new Document { Id = Guid.NewGuid(), Name = "second" }, cancellationToken: TestContext.Current.CancellationToken);
            session.AbortTransaction(TestContext.Current.CancellationToken);

            var remaining = collection.FindSync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);
            var doc = Assert.Single(remaining);
            Assert.Equal("first", doc.Name);
        }

        public class Document
        {
            [BsonId]
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
        }
    }
}
