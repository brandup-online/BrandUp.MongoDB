using System;
using MongoDB.Bson;
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
            var count = collection.CountDocuments(it => it.Name != "test", cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(0, count);
        }

        [Fact]
        public void CountDocuments_Empty()
        {
            var count = collection.CountDocuments(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertOne()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void UpdateOne()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var updateResult = collection.UpdateOne(it => it.Name == "test", Builders<Document>.Update.Set(it => it.Name, "test2"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, updateResult.MatchedCount);
            Assert.Equal(1, updateResult.ModifiedCount);

            var updatedDoc = collection.Find(it => it.Name == "test2").FirstOrDefault(TestContext.Current.CancellationToken);
            Assert.NotNull(updatedDoc);
            Assert.Equal(doc.Id, updatedDoc.Id);
            Assert.Equal("test2", updatedDoc.Name);
        }

        [Fact]
        public void UpdateMany()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);

            var updateResult = collection.UpdateMany(it => it.Name == "test", Builders<Document>.Update.Set(it => it.Name, "test2"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, updateResult.MatchedCount);
            Assert.Equal(2, updateResult.ModifiedCount);

            foreach (var updatedDoc in collection.Find(it => it.Name == "test2").ToList(TestContext.Current.CancellationToken))
                Assert.Equal("test2", updatedDoc.Name);
        }

        [Fact]
        public void ReplaceOne()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.ReplaceOne(it => it.Id == doc.Id, new Document { Id = doc.Id, Name = "test2" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, result.MatchedCount);
            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(1, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));

            var replacedDoc = collection.Find(it => it.Name == "test2").FirstOrDefault(TestContext.Current.CancellationToken);
            Assert.NotNull(replacedDoc);
            Assert.Equal(doc.Id, replacedDoc.Id);
            Assert.Equal("test2", replacedDoc.Name);
        }

        [Fact]
        public void DeleteOne()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.DeleteOne(it => it.Id == doc.Id, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, result.DeletedCount);
            Assert.Equal(0, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void DeleteMany()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.DeleteMany(it => it.Name == "test", cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, result.DeletedCount);
            Assert.Equal(0, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void FindSync_not_matched()
        {
            var result = collection.FindSync(it => it.Name == "test", cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Empty(result);
        }

        [Fact]
        public void FindSync_matched()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.FindSync(it => it.Name == "test", cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Single(result);
        }

        [Fact]
        public void FindSync_empty_filter()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "test" }, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.FindSync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Single(result);
        }

        [Fact]
        public void FindOneAndDelete_ReturnsDeletedDocument()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "test" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var deleted = collection.FindOneAndDelete(it => it.Id == doc.Id, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(deleted);
            Assert.Equal(doc.Id, deleted.Id);
            Assert.Equal(0, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void FindOneAndReplace_ReturnsBeforeByDefault()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "before" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var returned = collection.FindOneAndReplace(it => it.Id == doc.Id, new Document { Id = doc.Id, Name = "after" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("before", returned.Name);
            var current = collection.Find(it => it.Id == doc.Id).Single(TestContext.Current.CancellationToken);
            Assert.Equal("after", current.Name);
        }

        [Fact]
        public void FindOneAndReplace_ReturnAfter()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "before" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var returned = collection.FindOneAndReplace(
                it => it.Id == doc.Id,
                new Document { Id = doc.Id, Name = "after" },
                new FindOneAndReplaceOptions<Document, Document> { ReturnDocument = ReturnDocument.After },
                TestContext.Current.CancellationToken);

            Assert.Equal("after", returned.Name);
        }

        [Fact]
        public void FindOneAndUpdate_ReturnsBeforeByDefault()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "v1" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var returned = collection.FindOneAndUpdate(
                it => it.Id == doc.Id,
                Builders<Document>.Update.Set(it => it.Name, "v2"),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("v1", returned.Name);
            var current = collection.Find(it => it.Id == doc.Id).Single(TestContext.Current.CancellationToken);
            Assert.Equal("v2", current.Name);
        }

        [Fact]
        public void FindOneAndUpdate_ReturnAfter()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "v1" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var returned = collection.FindOneAndUpdate(
                it => it.Id == doc.Id,
                Builders<Document>.Update.Set(it => it.Name, "v2"),
                new FindOneAndUpdateOptions<Document, Document> { ReturnDocument = ReturnDocument.After },
                TestContext.Current.CancellationToken);

            Assert.Equal("v2", returned.Name);
        }

        [Fact]
        public void BulkWrite_RoutesInsertAndUpdate()
        {
            var existing = new Document { Id = Guid.NewGuid(), Name = "old" };
            collection.InsertOne(existing, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.BulkWrite([
                new InsertOneModel<Document>(new Document { Id = Guid.NewGuid(), Name = "new" }),
                new UpdateOneModel<Document>(
                    new ExpressionFilterDefinition<Document>(it => it.Id == existing.Id),
                    Builders<Document>.Update.Set(it => it.Name, "updated"))
            ], cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, result.InsertedCount);
            Assert.Equal(1, result.MatchedCount);
            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(2, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void BulkWrite_RoutesDelete()
        {
            var doc = new Document { Id = Guid.NewGuid(), Name = "x" };
            collection.InsertOne(doc, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.BulkWrite([
                new DeleteOneModel<Document>(new ExpressionFilterDefinition<Document>(it => it.Id == doc.Id))
            ], cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, result.DeletedCount);
            Assert.Equal(0, collection.EstimatedDocumentCount(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void OfType_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => collection.OfType<Document>());
        }

        [Fact]
        public void InsertOne_DuplicateId_ThrowsDuplicateKey()
        {
            var id = Guid.NewGuid();
            collection.InsertOne(new Document { Id = id, Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);

            var exception = Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new Document { Id = id, Name = "b" }, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(ServerErrorCategory.DuplicateKey, exception.WriteError.Category);
        }

        [Fact]
        public void UpdateOne_UnsupportedOperator_Throws()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Throws<NotSupportedException>(() =>
                collection.UpdateOne(it => it.Name == "a", Builders<Document>.Update.BitwiseAnd("Counter", 1), cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ReplaceOne_NoMatch_AcknowledgedZero()
        {
            var result = collection.ReplaceOne(it => it.Name == "missing", new Document { Id = Guid.NewGuid(), Name = "x" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.IsAcknowledged);
            Assert.Equal(0, result.MatchedCount);
            Assert.Equal(0, result.ModifiedCount);
        }

        [Fact]
        public void FindSync_RenderedFilter_Matches()
        {
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "x" }, cancellationToken: TestContext.Current.CancellationToken);
            collection.InsertOne(new Document { Id = Guid.NewGuid(), Name = "y" }, cancellationToken: TestContext.Current.CancellationToken);

            var result = collection.FindSync<Document>(Builders<Document>.Filter.Eq(it => it.Name, "x"), cancellationToken: TestContext.Current.CancellationToken).ToList(TestContext.Current.CancellationToken);

            Assert.Single(result);
            Assert.Equal("x", result[0].Name);
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