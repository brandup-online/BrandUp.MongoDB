using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionUniqueIndexTest
    {
        readonly IMongoCollection<Document> collection;

        public FakeMongoCollectionUniqueIndexTest()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("test");
            collection = db.GetCollection<Document>("test");
        }

        [Fact]
        public void Insert_DuplicateUniqueKey_ThrowsDuplicateKey()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Unique = true }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);

            var exception = Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(ServerErrorCategory.DuplicateKey, exception.WriteError.Category);
            Assert.Equal(11000, exception.WriteError.Code);
        }

        [Fact]
        public void Update_ViolatingUniqueKey_Throws()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Unique = true }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);
            var second = new Document { Id = Guid.NewGuid(), Name = "b" };
            collection.InsertOne(second, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<MongoWriteException>(() =>
                collection.UpdateOne(it => it.Id == second.Id, Builders<Document>.Update.Set(it => it.Name, "a"), cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void CaseInsensitiveCollation_PrimaryStrength()
        {
            // The SetupUsers pattern: case-insensitive unique email.
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Email),
                new CreateIndexOptions
                {
                    Unique = true,
                    Collation = new Collation("en", caseLevel: false, caseFirst: CollationCaseFirst.Off, strength: CollationStrength.Primary)
                }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Email = "User@X.io" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "b", Email = "user@x.io" }, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void SparseIndex_AllowsMissingValues()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Email),
                new CreateIndexOptions { Unique = true, Sparse = true }), cancellationToken: TestContext.Current.CancellationToken);

            // Two documents without the indexed field must not conflict.
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "b" }, cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "c", Email = "c@x.io" }, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "d", Email = "c@x.io" }, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void NonSparseIndex_TwoMissingValuesConflict()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Email),
                new CreateIndexOptions { Unique = true }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "b" }, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void PartialFilterExpression_LimitsUniqueness()
        {
            // The SetupPartner pattern: uniqueness only within a document subtype.
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions<Document>
                {
                    Unique = true,
                    PartialFilterExpression = Builders<Document>.Filter.Eq(it => it.Type, "special")
                }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "regular" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "regular" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "special" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "special" }, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void CompoundUniqueIndex()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name).Ascending(it => it.Type),
                new CreateIndexOptions { Unique = true }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "x" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "y" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a", Type = "x" }, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void BulkWrite_Unordered_DuplicateKey_ThrowsBulkExceptionAndAppliesRest()
        {
            // The SourceVisitStatRepository retry pattern: catch MongoBulkWriteException,
            // inspect WriteErrors categories and indices.
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Unique = true }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "taken" }, cancellationToken: TestContext.Current.CancellationToken);

            var exception = Assert.Throws<MongoBulkWriteException<Document>>(() =>
                collection.BulkWrite([
                    new InsertOneModel<Document>(new Document { Id = Guid.NewGuid(), Name = "taken" }),
                    new InsertOneModel<Document>(new Document { Id = Guid.NewGuid(), Name = "free" })
                ], new BulkWriteOptions { IsOrdered = false }, TestContext.Current.CancellationToken));

            Assert.True(exception.WriteErrors.All(it => it.Category == ServerErrorCategory.DuplicateKey));
            Assert.Equal(0, Assert.Single(exception.WriteErrors).Index);
            // Unordered: the second insert still went through.
            Assert.Equal(2, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void BulkWrite_Ordered_StopsAtFirstError()
        {
            collection.Indexes.CreateOne(new CreateIndexModel<Document>(
                Builders<Document>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Unique = true }), cancellationToken: TestContext.Current.CancellationToken);

            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "taken" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<MongoBulkWriteException<Document>>(() =>
                collection.BulkWrite([
                    new InsertOneModel<Document>(new Document { Id = Guid.NewGuid(), Name = "taken" }),
                    new InsertOneModel<Document>(new Document { Id = Guid.NewGuid(), Name = "free" })
                ], new BulkWriteOptions { IsOrdered = true }, TestContext.Current.CancellationToken));

            // Ordered: execution stopped, "free" was not inserted.
            Assert.Equal(1, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        public class Document
        {
            [BsonId]
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
            [BsonIgnoreIfNull]
            public string? Email { get; set; }
            [BsonIgnoreIfNull]
            public string? Type { get; set; }
        }
    }
}
