using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Contract
{
    /// <summary>
    /// Behavioural contract executed against both the in-memory fake and a real mongod
    /// (single-node replica set), so every supported scenario is verified to match
    /// real MongoDB semantics. Scenarios where the fake knowingly diverges
    /// (aggregations, change streams, array filters, snapshot isolation) are not here.
    /// </summary>
    public abstract class CollectionContractTest
    {
        protected abstract IMongoClient GetClientOrSkip();

        static CancellationToken CT => TestContext.Current.CancellationToken;

        IMongoCollection<ContractDocument> NewCollection()
        {
            return GetClientOrSkip().GetDatabase("ContractTests").GetCollection<ContractDocument>("c" + Guid.NewGuid().ToString("N"));
        }

        IMongoCollection<ContractSequenceDocument> NewSequenceCollection()
        {
            return GetClientOrSkip().GetDatabase("ContractTests").GetCollection<ContractSequenceDocument>("s" + Guid.NewGuid().ToString("N"));
        }

        /// <summary>Materializes the collection so transactional writes never have to create it implicitly.</summary>
        static void EnsureCreated(IMongoCollection<ContractDocument> collection)
        {
            var marker = new ContractDocument { Id = Guid.NewGuid(), Name = "__marker" };
            collection.InsertOne(marker, cancellationToken: CT);
            collection.DeleteMany(it => it.Id == marker.Id, CT);
        }

        static List<ContractDocument> Find(IMongoCollection<ContractDocument> collection, FilterDefinition<ContractDocument> filter)
        {
            return collection.FindSync(filter, new FindOptions<ContractDocument, ContractDocument>
            {
                Sort = Builders<ContractDocument>.Sort.Ascending(it => it.Name)
            }, CT).ToList(CT);
        }

        static IMongoCollection<ContractDocument> Seed(IMongoCollection<ContractDocument> collection)
        {
            collection.InsertMany([
                new ContractDocument { Id = Guid.NewGuid(), Name = "alpha", Counter = 1, Limit = 5, Type = "a", Tags = ["red", "green"], Items = [new ContractItem { Key = "k1", Score = 10 }], Profile = new ContractProfile { City = "NY" } },
                new ContractDocument { Id = Guid.NewGuid(), Name = "beta", Counter = 5, Limit = 3, Type = "b", Tags = ["blue"], Items = [new ContractItem { Key = "k2", Score = 3 }] },
                new ContractDocument { Id = Guid.NewGuid(), Name = "gamma", Counter = 10, Limit = 10, Type = "a", Tags = [], Items = [] }
            ], cancellationToken: CT);
            return collection;
        }

        #region Filters

        [Fact]
        public void Filter_Eq_Ne()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Name, "alpha")).Select(it => it.Name));
            Assert.Equal(["beta", "gamma"], Find(collection, Builders<ContractDocument>.Filter.Ne(it => it.Name, "alpha")).Select(it => it.Name));
        }

        [Fact]
        public void Filter_RangeComparisons()
        {
            var collection = Seed(NewCollection());
            var filter = Builders<ContractDocument>.Filter;

            Assert.Equal(2, Find(collection, filter.Gt(it => it.Counter, 1)).Count);
            Assert.Equal(3, Find(collection, filter.Gte(it => it.Counter, 1)).Count);
            Assert.Single(Find(collection, filter.Lt(it => it.Counter, 5)));
            Assert.Equal(2, Find(collection, filter.Lte(it => it.Counter, 5)).Count);
        }

        [Fact]
        public void Filter_In_Nin()
        {
            var collection = Seed(NewCollection());
            // A document without the field: $nin matches it, $in does not.
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "delta" }, cancellationToken: CT);

            Assert.Equal(2, Find(collection, Builders<ContractDocument>.Filter.In(it => it.Type, ["a"])).Count);
            Assert.Equal(["beta", "delta"], Find(collection, Builders<ContractDocument>.Filter.Nin(it => it.Type, ["a"])).Select(it => it.Name));
        }

        [Fact]
        public void Filter_Logical()
        {
            var collection = Seed(NewCollection());
            var filter = Builders<ContractDocument>.Filter;

            Assert.Equal(["beta"], Find(collection, filter.And(filter.Gt(it => it.Counter, 1), filter.Lt(it => it.Counter, 10))).Select(it => it.Name));
            Assert.Equal(["alpha", "gamma"], Find(collection, filter.Or(filter.Eq(it => it.Name, "alpha"), filter.Eq(it => it.Name, "gamma"))).Select(it => it.Name));
            Assert.Equal(["beta", "gamma"], Find(collection, filter.Not(filter.Eq(it => it.Name, "alpha"))).Select(it => it.Name));
        }

        [Fact]
        public void Filter_Exists()
        {
            var collection = Seed(NewCollection());
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "delta", Email = "d@x.io" }, cancellationToken: CT);

            Assert.Equal(["delta"], Find(collection, Builders<ContractDocument>.Filter.Exists(it => it.Email)).Select(it => it.Name));
            Assert.Equal(3, Find(collection, Builders<ContractDocument>.Filter.Exists(it => it.Email, false)).Count);
        }

        [Fact]
        public void Filter_Type()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(3, Find(collection, Builders<ContractDocument>.Filter.Type(it => it.Name, BsonType.String)).Count);
            Assert.Equal(3, Find(collection, Builders<ContractDocument>.Filter.Type(it => it.Counter, BsonType.Int32)).Count);
            Assert.Empty(Find(collection, Builders<ContractDocument>.Filter.Type(it => it.Name, BsonType.Int32)));
        }

        [Fact]
        public void Filter_Regex()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.Regex(it => it.Name, new BsonRegularExpression("^al"))).Select(it => it.Name));
            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.Regex(it => it.Name, new BsonRegularExpression("^AL", "i"))).Select(it => it.Name));
        }

        [Fact]
        public void Filter_Regex_OnArrayField()
        {
            var collection = Seed(NewCollection());

            var result = Find(collection, Builders<ContractDocument>.Filter.Regex("Tags", new BsonRegularExpression("^gre")));

            Assert.Equal(["alpha"], result.Select(it => it.Name));
        }

        [Fact]
        public void Filter_AnyEq()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.AnyEq(it => it.Tags, "red")).Select(it => it.Name));
        }

        [Fact]
        public void Filter_Size()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.Size(it => it.Tags, 2)).Select(it => it.Name));
        }

        [Fact]
        public void Filter_All()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.All(it => it.Tags, ["red", "green"])).Select(it => it.Name));
            // {$all: []} matches nothing.
            Assert.Empty(Find(collection, Builders<ContractDocument>.Filter.All(it => it.Tags, Array.Empty<string>())));
        }

        [Fact]
        public void Filter_ElemMatch()
        {
            var collection = Seed(NewCollection());

            var result = Find(collection, Builders<ContractDocument>.Filter.ElemMatch(it => it.Items, item => item.Score > 5));

            Assert.Equal(["alpha"], result.Select(it => it.Name));
        }

        [Fact]
        public void Filter_EqNull_MatchesMissingField()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(3, Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Email, null)).Count);
        }

        [Fact]
        public void Filter_Expr_ComparesTwoFields()
        {
            var collection = Seed(NewCollection());

            var filter = new BsonDocumentFilterDefinition<ContractDocument>(
                new BsonDocument("$expr", new BsonDocument("$lt", new BsonArray { "$Counter", "$Limit" })));

            Assert.Equal(["alpha"], Find(collection, filter).Select(it => it.Name));
        }

        [Fact]
        public void Filter_NestedPath()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(["alpha"], Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Profile!.City, "NY")).Select(it => it.Name));
        }

        [Fact]
        public void Find_Sort_Skip_Limit()
        {
            var collection = Seed(NewCollection());

            var result = collection.FindSync(Builders<ContractDocument>.Filter.Empty, new FindOptions<ContractDocument, ContractDocument>
            {
                Sort = Builders<ContractDocument>.Sort.Descending(it => it.Counter),
                Skip = 1,
                Limit = 1
            }, CT).ToList(CT);

            Assert.Equal(["beta"], result.Select(it => it.Name));
        }

        #endregion

        #region Updates

        [Fact]
        public void Update_Set_SecondIdenticalSet_ModifiesNothing()
        {
            var collection = Seed(NewCollection());

            var first = collection.UpdateOne(it => it.Name == "alpha", Builders<ContractDocument>.Update.Set(it => it.Type, "z"), cancellationToken: CT);
            var second = collection.UpdateOne(it => it.Name == "alpha", Builders<ContractDocument>.Update.Set(it => it.Type, "z"), cancellationToken: CT);

            Assert.Equal(1, first.MatchedCount);
            Assert.Equal(1, first.ModifiedCount);
            Assert.Equal(1, second.MatchedCount);
            Assert.Equal(0, second.ModifiedCount);
        }

        [Fact]
        public void Update_Inc_ExistingAndMissingField()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Counter = 1 };
            collection.InsertOne(doc, cancellationToken: CT);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Inc(it => it.Counter, 2).Inc(it => it.Total, 5), cancellationToken: CT);

            var updated = Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single();
            Assert.Equal(3, updated.Counter);
            Assert.Equal(5, updated.Total);
        }

        [Fact]
        public void Update_Unset()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Email = "a@x.io" };
            collection.InsertOne(doc, cancellationToken: CT);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Unset(it => it.Email), cancellationToken: CT);

            Assert.Empty(Find(collection, Builders<ContractDocument>.Filter.Exists(it => it.Email)));
        }

        [Fact]
        public void Update_MinMax()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Counter = 10 };
            collection.InsertOne(doc, cancellationToken: CT);
            var update = Builders<ContractDocument>.Update;

            collection.UpdateOne(it => it.Id == doc.Id, update.Min(it => it.Counter, 3), cancellationToken: CT);
            collection.UpdateOne(it => it.Id == doc.Id, update.Min(it => it.Counter, 7), cancellationToken: CT);
            Assert.Equal(3, Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Counter);

            collection.UpdateOne(it => it.Id == doc.Id, update.Max(it => it.Counter, 8), cancellationToken: CT);
            collection.UpdateOne(it => it.Id == doc.Id, update.Max(it => it.Counter, 5), cancellationToken: CT);
            Assert.Equal(8, Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Counter);
        }

        [Fact]
        public void Update_Mul()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Counter = 4 };
            collection.InsertOne(doc, cancellationToken: CT);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Mul(it => it.Counter, 3), cancellationToken: CT);

            Assert.Equal(12, Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Counter);
        }

        [Fact]
        public void Update_Rename()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Email = "value" };
            collection.InsertOne(doc, cancellationToken: CT);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Rename(it => it.Email, "Type"), cancellationToken: CT);

            var updated = Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single();
            Assert.Null(updated.Email);
            Assert.Equal("value", updated.Type);
        }

        [Fact]
        public void Update_AddToSet()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Tags = ["red"] };
            collection.InsertOne(doc, cancellationToken: CT);
            var update = Builders<ContractDocument>.Update;

            collection.UpdateOne(it => it.Id == doc.Id, update.AddToSet(it => it.Tags, "red"), cancellationToken: CT);
            collection.UpdateOne(it => it.Id == doc.Id, update.AddToSet(it => it.Tags, "blue"), cancellationToken: CT);

            Assert.Equal(new[] { "red", "blue" }, Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Tags);
        }

        [Fact]
        public void Update_Push_Pull()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Tags = ["red"] };
            collection.InsertOne(doc, cancellationToken: CT);
            var update = Builders<ContractDocument>.Update;

            collection.UpdateOne(it => it.Id == doc.Id, update.Push(it => it.Tags, "red"), cancellationToken: CT);
            Assert.Equal(new[] { "red", "red" }, Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Tags);

            collection.UpdateOne(it => it.Id == doc.Id, update.Pull(it => it.Tags, "red"), cancellationToken: CT);
            Assert.Empty(Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Tags);
        }

        [Fact]
        public void Update_NestedPath()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Profile = new ContractProfile { City = "NY" } };
            collection.InsertOne(doc, cancellationToken: CT);

            collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Set(it => it.Profile!.City, "LA"), cancellationToken: CT);

            Assert.Equal("LA", Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Profile!.City);
        }

        [Fact]
        public void UpdateMany_Counts()
        {
            var collection = Seed(NewCollection());

            var result = collection.UpdateMany(
                Builders<ContractDocument>.Filter.Eq(it => it.Type, "a"),
                Builders<ContractDocument>.Update.Set(it => it.Email, "bulk@x.io"),
                cancellationToken: CT);

            Assert.Equal(2, result.MatchedCount);
            Assert.Equal(2, result.ModifiedCount);
        }

        [Fact]
        public void Update_Upsert_SeedsFromFilterEqualities()
        {
            var collection = NewCollection();
            var id = Guid.NewGuid();
            var filter = Builders<ContractDocument>.Filter.And(
                Builders<ContractDocument>.Filter.Eq(it => it.Id, id),
                Builders<ContractDocument>.Filter.Eq(it => it.Name, "created"));
            var update = Builders<ContractDocument>.Update.Set(it => it.Counter, 7).SetOnInsert(it => it.Email, "new@x.io");

            var first = collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true }, CT);
            Assert.Equal(0, first.MatchedCount);
            Assert.NotNull(first.UpsertedId);

            var created = Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, id)).Single();
            Assert.Equal("created", created.Name);
            Assert.Equal(7, created.Counter);
            Assert.Equal("new@x.io", created.Email);

            // Second upsert matches: $setOnInsert must not apply.
            var second = collection.UpdateOne(filter, Builders<ContractDocument>.Update.Set(it => it.Counter, 8).SetOnInsert(it => it.Email, "other@x.io"), new UpdateOptions { IsUpsert = true }, CT);
            Assert.Equal(1, second.MatchedCount);
            Assert.Null(second.UpsertedId);
            Assert.Equal("new@x.io", Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, id)).Single().Email);
        }

        [Fact]
        public void FindOneAndUpdate_ReturnBeforeAndAfter()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "v1" };
            collection.InsertOne(doc, cancellationToken: CT);

            var before = collection.FindOneAndUpdate(
                it => it.Id == doc.Id,
                Builders<ContractDocument>.Update.Set(it => it.Name, "v2"),
                cancellationToken: CT);
            Assert.Equal("v1", before.Name);

            var after = collection.FindOneAndUpdate(
                it => it.Id == doc.Id,
                Builders<ContractDocument>.Update.Set(it => it.Name, "v3"),
                new FindOneAndUpdateOptions<ContractDocument, ContractDocument> { ReturnDocument = ReturnDocument.After },
                CT);
            Assert.Equal("v3", after.Name);
        }

        [Fact]
        public void FindOneAndUpdate_Sort_ClaimsOldest()
        {
            var collection = Seed(NewCollection());

            var claimed = collection.FindOneAndUpdate(
                Builders<ContractDocument>.Filter.Empty,
                Builders<ContractDocument>.Update.Set(it => it.Email, "claimed"),
                new FindOneAndUpdateOptions<ContractDocument, ContractDocument>
                {
                    Sort = Builders<ContractDocument>.Sort.Ascending(it => it.Counter),
                    ReturnDocument = ReturnDocument.After
                },
                CT);

            Assert.Equal("alpha", claimed.Name);
            Assert.Equal("claimed", claimed.Email);
        }

        [Fact]
        public void FindOneAndUpdate_Upsert_SequenceGenerator()
        {
            var collection = NewSequenceCollection();
            var options = new FindOneAndUpdateOptions<ContractSequenceDocument, ContractSequenceDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var first = collection.FindOneAndUpdate(it => it.Name == "invoices", Builders<ContractSequenceDocument>.Update.Inc(it => it.Counter, 1), options, CT);
            var second = collection.FindOneAndUpdate(it => it.Name == "invoices", Builders<ContractSequenceDocument>.Update.Inc(it => it.Counter, 1), options, CT);

            Assert.Equal(1, first.Counter);
            Assert.Equal(2, second.Counter);
            Assert.Equal("invoices", second.Name);
            Assert.NotEqual(ObjectId.Empty, second.Id);
            Assert.Equal(1, collection.CountDocuments(Builders<ContractSequenceDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void FindOneAndReplace_BeforeAndAfter()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "before" };
            collection.InsertOne(doc, cancellationToken: CT);

            var returnedBefore = collection.FindOneAndReplace(it => it.Id == doc.Id, new ContractDocument { Id = doc.Id, Name = "middle" }, cancellationToken: CT);
            Assert.Equal("before", returnedBefore.Name);

            var returnedAfter = collection.FindOneAndReplace(
                it => it.Id == doc.Id,
                new ContractDocument { Id = doc.Id, Name = "after" },
                new FindOneAndReplaceOptions<ContractDocument, ContractDocument> { ReturnDocument = ReturnDocument.After },
                CT);
            Assert.Equal("after", returnedAfter.Name);
        }

        [Fact]
        public void FindOneAndDelete_WithSort()
        {
            var collection = Seed(NewCollection());

            var deleted = collection.FindOneAndDelete(
                Builders<ContractDocument>.Filter.Empty,
                new FindOneAndDeleteOptions<ContractDocument, ContractDocument> { Sort = Builders<ContractDocument>.Sort.Descending(it => it.Counter) },
                CT);

            Assert.Equal("gamma", deleted.Name);
            Assert.Equal(2, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void Replace_Counts_And_NoMatch()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a" };
            collection.InsertOne(doc, cancellationToken: CT);

            var replaced = collection.ReplaceOne(it => it.Id == doc.Id, new ContractDocument { Id = doc.Id, Name = "b" }, new ReplaceOptions(), CT);
            Assert.Equal(1, replaced.MatchedCount);
            Assert.Equal(1, replaced.ModifiedCount);

            var noMatch = collection.ReplaceOne(it => it.Name == "missing", new ContractDocument { Id = Guid.NewGuid(), Name = "x" }, new ReplaceOptions(), CT);
            Assert.True(noMatch.IsAcknowledged);
            Assert.Equal(0, noMatch.MatchedCount);
            Assert.Equal(0, noMatch.ModifiedCount);
        }

        [Fact]
        public void Replace_IdenticalDocument_ModifiesNothing()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a" };
            collection.InsertOne(doc, cancellationToken: CT);

            var result = collection.ReplaceOne(it => it.Id == doc.Id, doc, new ReplaceOptions(), CT);

            Assert.Equal(1, result.MatchedCount);
            Assert.Equal(0, result.ModifiedCount);
        }

        [Fact]
        public void Replace_ChangedId_Throws()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a" };
            collection.InsertOne(doc, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.ReplaceOne(it => it.Id == doc.Id, new ContractDocument { Id = Guid.NewGuid(), Name = "b" }, new ReplaceOptions(), CT));
        }

        [Fact]
        public void Replace_Upsert()
        {
            var collection = NewCollection();

            var result = collection.ReplaceOne(
                it => it.Name == "missing",
                new ContractDocument { Id = Guid.NewGuid(), Name = "missing" },
                new ReplaceOptions { IsUpsert = true },
                CT);

            Assert.Equal(0, result.MatchedCount);
            Assert.NotNull(result.UpsertedId);
            Assert.Equal(1, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void Delete_OneOfMany_And_Many()
        {
            var collection = NewCollection();
            collection.InsertMany([
                new ContractDocument { Id = Guid.NewGuid(), Name = "x", Type = "dup" },
                new ContractDocument { Id = Guid.NewGuid(), Name = "y", Type = "dup" },
                new ContractDocument { Id = Guid.NewGuid(), Name = "z", Type = "other" }
            ], cancellationToken: CT);

            var one = collection.DeleteOne(it => it.Type == "dup", CT);
            Assert.Equal(1, one.DeletedCount);
            Assert.Equal(2, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));

            var many = collection.DeleteMany(Builders<ContractDocument>.Filter.Empty, CT);
            Assert.Equal(2, many.DeletedCount);
        }

        [Fact]
        public void BulkWrite_MixedCounts()
        {
            var collection = NewCollection();
            var existing = new ContractDocument { Id = Guid.NewGuid(), Name = "old" };
            collection.InsertOne(existing, cancellationToken: CT);

            var result = collection.BulkWrite([
                new InsertOneModel<ContractDocument>(new ContractDocument { Id = Guid.NewGuid(), Name = "new" }),
                new UpdateOneModel<ContractDocument>(
                    Builders<ContractDocument>.Filter.Eq(it => it.Id, existing.Id),
                    Builders<ContractDocument>.Update.Set(it => it.Name, "updated")),
                new DeleteOneModel<ContractDocument>(Builders<ContractDocument>.Filter.Eq(it => it.Name, "new"))
            ], cancellationToken: CT);

            Assert.Equal(1, result.InsertedCount);
            Assert.Equal(1, result.MatchedCount);
            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(1, result.DeletedCount);
        }

        [Fact]
        public void BulkWrite_Upserts_Reported()
        {
            var collection = NewSequenceCollection();
            collection.InsertOne(new ContractSequenceDocument { Id = ObjectId.GenerateNewId(), Name = "exists" }, cancellationToken: CT);

            var result = collection.BulkWrite([
                new UpdateOneModel<ContractSequenceDocument>(
                    Builders<ContractSequenceDocument>.Filter.Eq(it => it.Name, "exists"),
                    Builders<ContractSequenceDocument>.Update.Set(it => it.Counter, 1)) { IsUpsert = true },
                new UpdateOneModel<ContractSequenceDocument>(
                    Builders<ContractSequenceDocument>.Filter.Eq(it => it.Name, "new"),
                    Builders<ContractSequenceDocument>.Update.Set(it => it.Counter, 2)) { IsUpsert = true }
            ], new BulkWriteOptions { IsOrdered = false }, CT);

            Assert.Equal(1, result.MatchedCount);
            var upsert = Assert.Single(result.Upserts);
            Assert.Equal(1, upsert.Index);
            Assert.Equal(2, collection.CountDocuments(Builders<ContractSequenceDocument>.Filter.Empty, cancellationToken: CT));
        }

        #endregion

        #region Unique indexes

        static void CreateUniqueIndex(IMongoCollection<ContractDocument> collection, CreateIndexOptions? options = null, IndexKeysDefinition<ContractDocument>? keys = null)
        {
            options ??= new CreateIndexOptions();
            options.Unique = true;
            collection.Indexes.CreateOne(new CreateIndexModel<ContractDocument>(keys ?? Builders<ContractDocument>.IndexKeys.Ascending(it => it.Name), options), cancellationToken: CT);
        }

        [Fact]
        public void UniqueIndex_DuplicateInsert_Throws11000()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);

            var exception = Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT));
            Assert.Equal(ServerErrorCategory.DuplicateKey, exception.WriteError.Category);
            Assert.Equal(11000, exception.WriteError.Code);
        }

        [Fact]
        public void UniqueIndex_UpdateViolation_Throws()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            var second = new ContractDocument { Id = Guid.NewGuid(), Name = "b" };
            collection.InsertOne(second, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.UpdateOne(it => it.Id == second.Id, Builders<ContractDocument>.Update.Set(it => it.Name, "a"), cancellationToken: CT));
        }

        [Fact]
        public void UniqueIndex_UpdateKeepingOwnValue_DoesNotConflict()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);

            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a" };
            collection.InsertOne(doc, cancellationToken: CT);

            // Updating another field must not trip the unique check against the document itself.
            var result = collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Set(it => it.Counter, 5), cancellationToken: CT);

            Assert.Equal(1, result.ModifiedCount);
        }

        [Fact]
        public void UniqueIndex_Sparse()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection, new CreateIndexOptions { Sparse = true }, Builders<ContractDocument>.IndexKeys.Ascending(it => it.Email));

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "b" }, cancellationToken: CT);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "c", Email = "c@x.io" }, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "d", Email = "c@x.io" }, cancellationToken: CT));
        }

        [Fact]
        public void UniqueIndex_NonSparse_TwoMissingValuesConflict()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection, keys: Builders<ContractDocument>.IndexKeys.Ascending(it => it.Email));

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "b" }, cancellationToken: CT));
        }

        [Fact]
        public void UniqueIndex_PartialFilter()
        {
            var collection = NewCollection();
            collection.Indexes.CreateOne(new CreateIndexModel<ContractDocument>(
                Builders<ContractDocument>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions<ContractDocument>
                {
                    Unique = true,
                    PartialFilterExpression = Builders<ContractDocument>.Filter.Eq(it => it.Type, "special")
                }), cancellationToken: CT);

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "regular" }, cancellationToken: CT);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "regular" }, cancellationToken: CT);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "special" }, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "special" }, cancellationToken: CT));
        }

        [Fact]
        public void UniqueIndex_Compound()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection, keys: Builders<ContractDocument>.IndexKeys.Ascending(it => it.Name).Ascending(it => it.Type));

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "x" }, cancellationToken: CT);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "y" }, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "x" }, cancellationToken: CT));
        }

        [Fact]
        public void UniqueIndex_CaseInsensitiveCollation()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection, new CreateIndexOptions
            {
                Collation = new Collation("en", caseLevel: false, caseFirst: CollationCaseFirst.Off, strength: CollationStrength.Primary)
            }, Builders<ContractDocument>.IndexKeys.Ascending(it => it.Email));

            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a", Email = "User@X.io" }, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "b", Email = "user@x.io" }, cancellationToken: CT));
        }

        [Fact]
        public void Index_RecreateSameSpec_IsNoOp()
        {
            var collection = NewCollection();
            var model = new CreateIndexModel<ContractDocument>(
                Builders<ContractDocument>.IndexKeys.Ascending(it => it.Name),
                new CreateIndexOptions { Name = "name_idx" });

            collection.Indexes.CreateOne(model, cancellationToken: CT);
            collection.Indexes.CreateOne(model, cancellationToken: CT);

            Assert.Single(collection.Indexes.List(CT).ToList(CT), it => it["name"] == "name_idx");
        }

        [Fact]
        public void Index_RecreateUnnamedSameKeys_IsNoOp()
        {
            var collection = NewCollection();
            EnsureCreated(collection);

            var name1 = collection.Indexes.CreateOne(new CreateIndexModel<ContractDocument>(Builders<ContractDocument>.IndexKeys.Ascending(it => it.Name)), cancellationToken: CT);
            var name2 = collection.Indexes.CreateOne(new CreateIndexModel<ContractDocument>(Builders<ContractDocument>.IndexKeys.Ascending(it => it.Name)), cancellationToken: CT);

            Assert.Equal("Name_1", name1);
            Assert.Equal(name1, name2);
            Assert.Single(collection.Indexes.List(CT).ToList(CT), it => it["name"] == "Name_1");
        }

        [Fact]
        public void Update_Set_ScalarIntermediatePath_Throws()
        {
            // The server refuses to implicitly turn an existing scalar into a nested document.
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Type = "scalar" };
            collection.InsertOne(doc, cancellationToken: CT);

            Assert.ThrowsAny<MongoException>(() =>
                collection.UpdateOne(it => it.Id == doc.Id,
                    Builders<ContractDocument>.Update.Set("Type.Nested", 1),
                    cancellationToken: CT));
        }

        [Fact]
        public void InsertMany_Ordered_Duplicate_ThrowsBulkExceptionAndStops()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "taken" }, cancellationToken: CT);

            var exception = Assert.Throws<MongoBulkWriteException<ContractDocument>>(() =>
                collection.InsertMany([
                    new ContractDocument { Id = Guid.NewGuid(), Name = "taken" },
                    new ContractDocument { Id = Guid.NewGuid(), Name = "free" }
                ], cancellationToken: CT));

            var error = Assert.Single(exception.WriteErrors);
            Assert.Equal(ServerErrorCategory.DuplicateKey, error.Category);
            // Ordered by default: "free" must not have been inserted.
            Assert.Equal(1, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void FindOneAndUpdate_DuplicateKey_ThrowsCommandException()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            var second = new ContractDocument { Id = Guid.NewGuid(), Name = "b" };
            collection.InsertOne(second, cancellationToken: CT);

            // findAndModify surfaces the duplicate as a command error, not a write error.
            var exception = Assert.Throws<MongoCommandException>(() =>
                collection.FindOneAndUpdate(it => it.Id == second.Id, Builders<ContractDocument>.Update.Set(it => it.Name, "a"), cancellationToken: CT));
            Assert.Equal(11000, exception.Code);
        }

        [Fact]
        public void Update_PushOnNullField_Throws()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Tags = null! };
            collection.InsertOne(doc, cancellationToken: CT);

            Assert.Throws<MongoWriteException>(() =>
                collection.UpdateOne(it => it.Id == doc.Id, Builders<ContractDocument>.Update.Push(it => it.Tags, "x"), cancellationToken: CT));
        }

        [Fact]
        public void Index_List_ContainsIdIndex()
        {
            var collection = NewCollection();
            EnsureCreated(collection);

            var indexes = collection.Indexes.List(CT).ToList(CT);

            Assert.Single(indexes, it => it["name"] == "_id_");
        }

        [Fact]
        public void Update_Pull_SecondFieldStillApplied_WhenFirstMissing()
        {
            var collection = NewCollection();
            var doc = new ContractDocument { Id = Guid.NewGuid(), Name = "a", Tags = ["red", "blue"] };
            collection.InsertOne(doc, cancellationToken: CT);

            // "Missing" is absent on the document; Tags must still be pulled from.
            var update = new BsonDocumentUpdateDefinition<ContractDocument>(
                new BsonDocument("$pull", new BsonDocument { { "Missing", "x" }, { "Tags", "red" } }));
            collection.UpdateOne(it => it.Id == doc.Id, update, cancellationToken: CT);

            Assert.Equal(["blue"], Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, doc.Id)).Single().Tags);
        }

        [Fact]
        public void BulkWrite_Unordered_DuplicateKey_AppliesRestAndReportsError()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "taken" }, cancellationToken: CT);

            var exception = Assert.Throws<MongoBulkWriteException<ContractDocument>>(() =>
                collection.BulkWrite([
                    new InsertOneModel<ContractDocument>(new ContractDocument { Id = Guid.NewGuid(), Name = "taken" }),
                    new InsertOneModel<ContractDocument>(new ContractDocument { Id = Guid.NewGuid(), Name = "free" })
                ], new BulkWriteOptions { IsOrdered = false }, CT));

            var error = Assert.Single(exception.WriteErrors);
            Assert.Equal(0, error.Index);
            Assert.Equal(ServerErrorCategory.DuplicateKey, error.Category);
            Assert.Equal(2, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void BulkWrite_Ordered_StopsAtFirstError()
        {
            var collection = NewCollection();
            CreateUniqueIndex(collection);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "taken" }, cancellationToken: CT);

            Assert.Throws<MongoBulkWriteException<ContractDocument>>(() =>
                collection.BulkWrite([
                    new InsertOneModel<ContractDocument>(new ContractDocument { Id = Guid.NewGuid(), Name = "taken" }),
                    new InsertOneModel<ContractDocument>(new ContractDocument { Id = Guid.NewGuid(), Name = "free" })
                ], new BulkWriteOptions { IsOrdered = true }, CT));

            Assert.Equal(1, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        #endregion

        #region Distinct and counts

        [Fact]
        public void Distinct_Values()
        {
            var collection = Seed(NewCollection());

            var values = collection.Distinct(it => it.Type, Builders<ContractDocument>.Filter.Empty, cancellationToken: CT).ToList(CT);

            Assert.Equal(["a", "b"], values.Order());
        }

        [Fact]
        public void Distinct_ArrayField_FlattensValues()
        {
            var collection = Seed(NewCollection());

            var values = collection.Distinct<string>("Tags", Builders<ContractDocument>.Filter.Empty, cancellationToken: CT).ToList(CT);

            Assert.Equal(["blue", "green", "red"], values.Order());
        }

        [Fact]
        public void Distinct_WithFilter()
        {
            var collection = Seed(NewCollection());

            var values = collection.Distinct(it => it.Type, Builders<ContractDocument>.Filter.Gte(it => it.Counter, 5), cancellationToken: CT).ToList(CT);

            Assert.Equal(["a", "b"], values.Order());
        }

        [Fact]
        public void CountDocuments_Filter_Skip_Limit()
        {
            var collection = Seed(NewCollection());

            Assert.Equal(2, collection.CountDocuments(Builders<ContractDocument>.Filter.Gte(it => it.Counter, 5), cancellationToken: CT));
            Assert.Equal(1, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, new CountOptions { Skip = 1, Limit = 1 }, CT));
        }

        #endregion

        #region Transactions

        [Fact]
        public void Transaction_Abort_RollsBackInsert()
        {
            var collection = NewCollection();
            EnsureCreated(collection);
            using var session = GetClientOrSkip().StartSession(cancellationToken: CT);

            session.StartTransaction();
            collection.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            Assert.Equal(1, collection.CountDocuments(session, Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
            session.AbortTransaction(CT);

            Assert.Equal(0, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void Transaction_Commit_KeepsChanges()
        {
            var collection = NewCollection();
            EnsureCreated(collection);
            using var session = GetClientOrSkip().StartSession(cancellationToken: CT);

            session.StartTransaction();
            collection.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            session.CommitTransaction(CT);

            Assert.Equal(1, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void Transaction_Abort_RollsBackUpdateAndDelete()
        {
            var collection = NewCollection();
            var kept = new ContractDocument { Id = Guid.NewGuid(), Name = "kept" };
            var removed = new ContractDocument { Id = Guid.NewGuid(), Name = "removed" };
            collection.InsertOne(kept, cancellationToken: CT);
            collection.InsertOne(removed, cancellationToken: CT);

            using (var session = GetClientOrSkip().StartSession(cancellationToken: CT))
            {
                session.StartTransaction();
                collection.UpdateOne(session, it => it.Id == kept.Id, Builders<ContractDocument>.Update.Set(it => it.Name, "changed"), cancellationToken: CT);
                collection.DeleteOne(session, it => it.Id == removed.Id, cancellationToken: CT);
                session.AbortTransaction(CT);
            }

            Assert.Equal(2, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
            Assert.Equal("kept", Find(collection, Builders<ContractDocument>.Filter.Eq(it => it.Id, kept.Id)).Single().Name);
        }

        [Fact]
        public void Transaction_DisposeWithoutCommit_RollsBack()
        {
            var collection = NewCollection();
            EnsureCreated(collection);

            using (var session = GetClientOrSkip().StartSession(cancellationToken: CT))
            {
                session.StartTransaction();
                collection.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            }

            Assert.Equal(0, collection.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void Transaction_SessionlessWrite_SurvivesAbort()
        {
            var collection = NewCollection();
            EnsureCreated(collection);
            using var session = GetClientOrSkip().StartSession(cancellationToken: CT);

            session.StartTransaction();
            collection.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "inside" }, cancellationToken: CT);
            collection.InsertOne(new ContractDocument { Id = Guid.NewGuid(), Name = "outside" }, cancellationToken: CT);
            session.AbortTransaction(CT);

            var remaining = Find(collection, Builders<ContractDocument>.Filter.Empty);
            Assert.Equal(["outside"], remaining.Select(it => it.Name));
        }

        [Fact]
        public void Transaction_TwoCollections_BothRolledBack()
        {
            var first = NewCollection();
            var second = NewCollection();
            EnsureCreated(first);
            EnsureCreated(second);
            using var session = GetClientOrSkip().StartSession(cancellationToken: CT);

            session.StartTransaction();
            first.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "a" }, cancellationToken: CT);
            second.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "b" }, cancellationToken: CT);
            session.AbortTransaction(CT);

            Assert.Equal(0, first.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
            Assert.Equal(0, second.CountDocuments(Builders<ContractDocument>.Filter.Empty, cancellationToken: CT));
        }

        [Fact]
        public void Transaction_Sequential_OnSameSession()
        {
            var collection = NewCollection();
            EnsureCreated(collection);
            using var session = GetClientOrSkip().StartSession(cancellationToken: CT);

            session.StartTransaction();
            collection.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "first" }, cancellationToken: CT);
            session.CommitTransaction(CT);

            session.StartTransaction();
            collection.InsertOne(session, new ContractDocument { Id = Guid.NewGuid(), Name = "second" }, cancellationToken: CT);
            session.AbortTransaction(CT);

            var remaining = Find(collection, Builders<ContractDocument>.Filter.Empty);
            Assert.Equal(["first"], remaining.Select(it => it.Name));
        }

        #endregion
    }
}
