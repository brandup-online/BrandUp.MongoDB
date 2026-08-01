using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandUp.MongoDB.Testing.Internals;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoCollection<TDocument> : IMongoCollection<TDocument>, IFakeMongoCollection
    {
        readonly FakeMongoDatabase database;
        readonly List<BsonDocument> docs = new List<BsonDocument>();
        readonly Dictionary<BsonValue, int> docIds = new Dictionary<BsonValue, int>();
        readonly List<TDocument> docObjects = new List<TDocument>();
        readonly FakeMongoIndexManager<TDocument> indexManager;
        readonly FakeMongoSearchIndexManager searchIndexManager = new();

        public FakeMongoCollection(FakeMongoDatabase database, string name, MongoCollectionSettings settings)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CollectionNamespace = new CollectionNamespace(database.DatabaseNamespace, name);
            DocumentSerializer = BsonSerializer.LookupSerializer<TDocument>();
            indexManager = new FakeMongoIndexManager<TDocument>(this);
        }

        public CollectionNamespace CollectionNamespace { get; }
        public IMongoDatabase Database => database;
        public IBsonSerializer<TDocument> DocumentSerializer { get; }
        public IMongoIndexManager<TDocument> Indexes => indexManager;
        public MongoCollectionSettings Settings { get; }

        public IMongoSearchIndexManager SearchIndexes => searchIndexManager;

        public IQueryable<TDocument> GetDocuemntsQueryable()
        {
            return docObjects.AsQueryable();
        }

        #region Helpers

        RenderArgs<TDocument> RenderArgs => new RenderArgs<TDocument>(DocumentSerializer, Settings.SerializerRegistry);

        /// <summary>
        /// Resolves a filter to the indices of matching documents (in insertion order).
        /// Expression filters are compiled and evaluated against the CLR objects; any other
        /// filter is rendered to BSON and evaluated by the matcher.
        /// </summary>
        List<int> FindMatchingIndices(FilterDefinition<TDocument> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter is ExpressionFilterDefinition<TDocument> expressionFilter)
            {
                var predicate = expressionFilter.Expression.Compile();
                var matched = new List<int>();
                for (var i = 0; i < docObjects.Count; i++)
                {
                    if (predicate(docObjects[i]))
                        matched.Add(i);
                }
                return matched;
            }

            var filterDoc = filter.Render(RenderArgs);
            if (filterDoc.ElementCount == 0)
                return Enumerable.Range(0, docs.Count).ToList();

            var result = new List<int>();
            for (var i = 0; i < docs.Count; i++)
            {
                if (BsonFilterMatcher.Matches(filterDoc, docs[i]))
                    result.Add(i);
            }
            return result;
        }

        void SortIndices(List<int> indices, SortDefinition<TDocument>? sort)
        {
            if (sort == null)
                return;

            var sortDoc = sort.Render(RenderArgs);
            if (sortDoc.ElementCount == 0)
                return;

            var comparer = new BsonSortComparer(sortDoc);
            indices.Sort((a, b) => comparer.Compare(docs[a], docs[b]));
        }

        BsonDocument SerializeDocument(TDocument document)
        {
            var bsonDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(bsonDocument))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                DocumentSerializer.Serialize(context, new BsonSerializationArgs { SerializeIdFirst = true }, document);
            }
            return bsonDocument;
        }

        TDocument DeserializeDocument(BsonDocument document)
        {
            using var bsonReader = new BsonDocumentReader(document);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);
            return DocumentSerializer.Deserialize(context);
        }

        void RecordUndo(IClientSessionHandle? session, Action undo)
        {
            if (session is FakeClientSessionHandle fakeSession)
                fakeSession.RecordUndo(undo);
        }

        void RemoveById(BsonValue id)
        {
            if (!docIds.TryGetValue(id, out var index))
                return;

            docs.RemoveAt(index);
            docObjects.RemoveAt(index);
            ReindexIds();
        }

        void ReindexIds()
        {
            docIds.Clear();
            for (var i = 0; i < docs.Count; i++)
                docIds.Add(GetDocumentIdValue(docs[i]), i);
        }

        void EnsureUniqueIndexes(BsonDocument candidate, int? selfIndex)
        {
            foreach (var index in indexManager.UniqueIndexes)
            {
                if (!index.Participates(candidate))
                    continue;

                var key = index.ExtractKey(candidate);
                for (var i = 0; i < docs.Count; i++)
                {
                    if (i == selfIndex)
                        continue;

                    var other = docs[i];
                    if (!index.Participates(other))
                        continue;

                    if (index.KeysEqual(key, index.ExtractKey(other)))
                        throw DriverExceptionFactory.CreateDuplicateKeyException(CollectionNamespace, index.Name, new BsonArray(key));
                }
            }
        }

        BsonValue InsertDocument(IClientSessionHandle? session, TDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var bsonDocument = SerializeDocument(document);

            var id = GetDocumentIdValue(bsonDocument);
            if (docIds.ContainsKey(id))
                throw DriverExceptionFactory.CreateDuplicateKeyException(CollectionNamespace, "_id_", id);

            EnsureUniqueIndexes(bsonDocument, null);

            var index = docs.Count;
            docIds.Add(id, index);
            docs.Add(bsonDocument);
            docObjects.Add(document);

            RecordUndo(session, () => RemoveById(id));

            return id;
        }

        /// <summary>
        /// Creates a new document for an upsert: deserializes the composed BSON, assigns a
        /// generated id when the source has none (mirroring driver-side id generation) and inserts.
        /// </summary>
        BsonValue UpsertInsert(IClientSessionHandle? session, BsonDocument source, out TDocument inserted)
        {
            var hadId = source.Contains("_id");
            inserted = DeserializeDocument(source);

            if (!hadId)
                TryAssignGeneratedId(inserted);

            return InsertDocument(session, inserted);
        }

        void TryAssignGeneratedId(TDocument document)
        {
            if (document is BsonDocument bsonDocument)
            {
                if (!bsonDocument.Contains("_id"))
                    bsonDocument.InsertAt(0, new BsonElement("_id", ObjectId.GenerateNewId()));
                return;
            }

            try
            {
                var classMap = BsonClassMap.LookupClassMap(typeof(TDocument));
                var idMap = classMap.IdMemberMap;
                if (idMap?.IdGenerator is { } generator)
                {
                    var currentId = idMap.Getter(document!);
                    if (generator.IsEmpty(currentId))
                        idMap.Setter(document!, generator.GenerateId(this, document!));
                }
            }
            catch
            {
                // Types without a class map keep whatever id the deserialized document carries.
            }
        }

        static void EnsureNoArrayFilters(IEnumerable<ArrayFilterDefinition>? arrayFilters)
        {
            if (arrayFilters != null && arrayFilters.Any())
                throw new NotSupportedException("Array filters are not supported by the in-memory fake.");
        }

        static void EnsureNoCollation(Collation? collation)
        {
            if (collation != null)
                throw new NotSupportedException("Query-level collations are not supported by the in-memory fake; collations are only honoured on unique indexes.");
        }

        /// <summary>Applies the update engine, reporting its failures as write errors the way a server does.</summary>
        static void ApplyUpdateEngine(BsonDocument updateDoc, BsonDocument target, bool insertMode)
        {
            try
            {
                BsonUpdateEngine.Apply(updateDoc, target, insertMode);
            }
            catch (InvalidOperationException exception)
            {
                throw DriverExceptionFactory.CreateUpdateErrorException(exception.Message);
            }
        }

        UpdateResult ApplyUpdate(IClientSessionHandle? session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options, bool updateManyDocuments, SortDefinition<TDocument>? sort = null)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentNullException.ThrowIfNull(update);

            EnsureNoArrayFilters(options?.ArrayFilters);
            EnsureNoCollation(options?.Collation);

            var renderedUpdate = update.Render(RenderArgs);
            if (renderedUpdate is not BsonDocument updateDoc)
                throw new NotSupportedException("Aggregation-pipeline updates are not supported by the in-memory fake.");

            var indices = FindMatchingIndices(filter);

            if (indices.Count == 0)
            {
                if (options?.IsUpsert == true)
                {
                    var seed = BsonUpdateEngine.BuildUpsertSeed(RenderFilterForUpsert(filter));
                    ApplyUpdateEngine(updateDoc, seed, insertMode: true);
                    var upsertedId = UpsertInsert(session, seed, out _);
                    return new UpdateResult.Acknowledged(0, 0, upsertedId);
                }

                return new UpdateResult.Acknowledged(0, 0, null);
            }

            if (!updateManyDocuments && indices.Count > 1)
            {
                SortIndices(indices, sort);
                indices = [indices[0]];
            }

            long matchedCount = 0, modifiedCount = 0;
            foreach (var docIndex in indices)
            {
                matchedCount++;
                if (UpdateDocumentAt(session, docIndex, updateDoc))
                    modifiedCount++;
            }

            return new UpdateResult.Acknowledged(matchedCount, modifiedCount, null);
        }

        /// <summary>Applies a rendered update to the document at the given index. Returns true when the document changed.</summary>
        bool UpdateDocumentAt(IClientSessionHandle? session, int docIndex, BsonDocument updateDoc)
        {
            var doc = docs[docIndex];
            var docId = GetDocumentIdValue(doc);

            var updatedDoc = doc.DeepClone().AsBsonDocument;
            ApplyUpdateEngine(updateDoc, updatedDoc, insertMode: false);

            if (updatedDoc.Equals(doc))
                return false;

            var updatedId = GetDocumentIdValue(updatedDoc);
            if (updatedId != docId && docIds.ContainsKey(updatedId))
                throw DriverExceptionFactory.CreateDuplicateKeyException(CollectionNamespace, "_id_", updatedId);

            EnsureUniqueIndexes(updatedDoc, docIndex);

            var updatedObject = DeserializeDocument(updatedDoc);

            if (updatedId != docId)
            {
                if (!docIds.Remove(docId))
                    throw new InvalidOperationException();
                docIds.Add(updatedId, docIndex);
            }

            var previousObject = docObjects[docIndex];

            docs[docIndex] = updatedDoc;
            docObjects[docIndex] = updatedObject;

            RecordUndo(session, () =>
            {
                if (!docIds.TryGetValue(updatedId, out var currentIndex))
                    return;

                docs[currentIndex] = doc;
                docObjects[currentIndex] = previousObject;
                if (updatedId != docId)
                {
                    docIds.Remove(updatedId);
                    docIds[docId] = currentIndex;
                }
            });

            return true;
        }

        BsonDocument RenderFilterForUpsert(FilterDefinition<TDocument> filter)
        {
            try
            {
                return filter.Render(RenderArgs);
            }
            catch (Exception exception) when (filter is ExpressionFilterDefinition<TDocument>)
            {
                throw new NotSupportedException("Cannot build an upsert document from this expression filter.", exception);
            }
        }

        DeleteResult DeleteDocuments(IClientSessionHandle? session, List<int> indices)
        {
            var removed = new List<(BsonValue Id, BsonDocument Doc, TDocument Obj)>();

            foreach (var docIndex in indices.OrderByDescending(it => it))
            {
                var doc = docs[docIndex];
                var docId = GetDocumentIdValue(doc);

                if (!docIds.Remove(docId))
                    throw new InvalidOperationException();
                removed.Add((docId, doc, docObjects[docIndex]));
                docs.RemoveAt(docIndex);
                docObjects.RemoveAt(docIndex);
            }

            ReindexIds();

            if (removed.Count > 0)
            {
                RecordUndo(session, () =>
                {
                    foreach (var (id, doc, obj) in removed)
                    {
                        if (docIds.ContainsKey(id))
                            continue;

                        docIds.Add(id, docs.Count);
                        docs.Add(doc);
                        docObjects.Add(obj);
                    }
                });
            }

            return new DeleteResult.Acknowledged(indices.Count);
        }

        static BsonValue GetDocumentIdValue(BsonDocument document)
        {
            if (!document.TryGetValue("_id", out BsonValue idValue))
                throw new InvalidOperationException("Not found id value in bson document.");
            return idValue;
        }

        static TProjection AsProjection<TProjection>(TDocument document)
        {
            return (TProjection)(object)document!;
        }

        static TField DeserializeValue<TField>(IBsonSerializer<TField> serializer, BsonValue value)
        {
            var wrapper = new BsonDocument("v", value);
            using var reader = new BsonDocumentReader(wrapper);
            reader.ReadStartDocument();
            reader.ReadName();
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = serializer.Deserialize(context);
            reader.ReadEndDocument();
            return result;
        }

        IAsyncCursor<TField> DistinctValues<TField>(IBsonSerializer<TField> valueSerializer, string fieldName, FilterDefinition<TDocument> filter)
        {
            var indices = FindMatchingIndices(filter);
            var seen = new List<BsonValue>();
            var results = new List<TField>();

            foreach (var docIndex in indices)
            {
                var terminals = BsonValueHelper.ResolvePath(docs[docIndex], fieldName, out _);
                foreach (var terminal in terminals)
                {
                    var values = terminal is BsonArray array ? array.AsEnumerable() : [terminal];
                    foreach (var value in values)
                    {
                        if (seen.Any(it => BsonValueHelper.ValuesEqual(it, value)))
                            continue;

                        seen.Add(value);
                        results.Add(DeserializeValue(valueSerializer, value));
                    }
                }
            }

            return new FakeAsyncCursor<TField>(results);
        }

        #endregion

        #region Aggregate members

        public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }
        public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }
        public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }
        public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        #endregion

        #region BulkWrite members

        public BulkWriteResult<TDocument> BulkWrite(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            return BulkWrite(null!, requests, options, cancellationToken);
        }
        public BulkWriteResult<TDocument> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requests);

            var models = requests.ToList();
            var isOrdered = options?.IsOrdered ?? true;

            long matchedCount = 0, deletedCount = 0, insertedCount = 0, modifiedCount = 0;
            var upserts = new List<BulkWriteUpsert>();
            var errors = new List<BulkWriteError>();
            var processed = new List<WriteModel<TDocument>>();
            var stoppedAt = models.Count;

            for (var i = 0; i < models.Count; i++)
            {
                var request = models[i];
                try
                {
                    switch (request)
                    {
                        case InsertOneModel<TDocument> insert:
                            InsertDocument(session, insert.Document);
                            insertedCount++;
                            break;
                        case DeleteOneModel<TDocument> deleteOne:
                            EnsureNoCollation(deleteOne.Collation);
                            deletedCount += DeleteOne(session, deleteOne.Filter, null, cancellationToken).DeletedCount;
                            break;
                        case DeleteManyModel<TDocument> deleteMany:
                            EnsureNoCollation(deleteMany.Collation);
                            deletedCount += DeleteMany(session, deleteMany.Filter, null, cancellationToken).DeletedCount;
                            break;
                        case UpdateOneModel<TDocument> updateOne:
                            {
                                EnsureNoArrayFilters(updateOne.ArrayFilters);
                                EnsureNoCollation(updateOne.Collation);
                                var result = ApplyUpdate(session, updateOne.Filter, updateOne.Update, new UpdateOptions { IsUpsert = updateOne.IsUpsert }, updateManyDocuments: false, updateOne.Sort);
                                matchedCount += result.MatchedCount;
                                modifiedCount += result.ModifiedCount;
                                if (result.UpsertedId != null)
                                    upserts.Add(DriverExceptionFactory.CreateBulkWriteUpsert(i, result.UpsertedId));
                                break;
                            }
                        case UpdateManyModel<TDocument> updateMany:
                            {
                                EnsureNoArrayFilters(updateMany.ArrayFilters);
                                EnsureNoCollation(updateMany.Collation);
                                var result = ApplyUpdate(session, updateMany.Filter, updateMany.Update, new UpdateOptions { IsUpsert = updateMany.IsUpsert }, updateManyDocuments: true);
                                matchedCount += result.MatchedCount;
                                modifiedCount += result.ModifiedCount;
                                if (result.UpsertedId != null)
                                    upserts.Add(DriverExceptionFactory.CreateBulkWriteUpsert(i, result.UpsertedId));
                                break;
                            }
                        case ReplaceOneModel<TDocument> replace:
                            {
                                var result = ReplaceOneInternal(session, replace.Filter, replace.Replacement, replace.IsUpsert, replace.Collation);
                                if (result.IsAcknowledged)
                                {
                                    matchedCount += result.MatchedCount;
                                    modifiedCount += result.ModifiedCount;
                                    if (result.UpsertedId != null)
                                        upserts.Add(DriverExceptionFactory.CreateBulkWriteUpsert(i, result.UpsertedId));
                                }
                                break;
                            }
                        default:
                            throw new NotSupportedException($"Write model {request.GetType().Name} is not supported by the in-memory fake.");
                    }

                    processed.Add(request);
                }
                catch (MongoWriteException writeException) when (writeException.WriteError != null)
                {
                    errors.Add(DriverExceptionFactory.CreateBulkError(i, writeException.WriteError));

                    if (isOrdered)
                    {
                        stoppedAt = i + 1;
                        break;
                    }
                }
            }

            var bulkResult = new BulkWriteResult<TDocument>.Acknowledged(
                models.Count,
                matchedCount,
                deletedCount,
                insertedCount,
                modifiedCount,
                processed,
                upserts);

            if (errors.Count > 0)
                throw DriverExceptionFactory.CreateBulkWriteException(bulkResult, errors, models.Skip(stoppedAt));

            return bulkResult;
        }
        public Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            return BulkWriteAsync(null!, requests, options, cancellationToken);
        }
        public Task<BulkWriteResult<TDocument>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BulkWrite(session, requests, options, cancellationToken));
        }

        #endregion

        #region Count methods

        public long Count(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Count(null!, filter, options, cancellationToken);
        }

        public long Count(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return CountDocuments(session, filter, options, cancellationToken);
        }

        public Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return CountAsync(null!, filter, options, cancellationToken);
        }

        public Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Count(session, filter, options, cancellationToken));
        }

        #endregion

        #region CountDocuments methods

        public long CountDocuments(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return CountDocuments(null!, filter, options, cancellationToken);
        }
        public long CountDocuments(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNoCollation(options?.Collation);

            long count = FindMatchingIndices(filter).Count;

            if (options?.Skip != null)
                count = Math.Max(0, count - options.Skip.Value);
            if (options?.Limit != null)
                count = Math.Min(count, options.Limit.Value);

            return count;
        }
        public Task<long> CountDocumentsAsync(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return CountDocumentsAsync(null!, filter, options, cancellationToken);
        }
        public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CountDocuments(session, filter, options, cancellationToken));
        }

        #endregion

        #region DeleteMany members

        public DeleteResult DeleteMany(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return DeleteMany(null!, filter, null!, cancellationToken);
        }
        public DeleteResult DeleteMany(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return DeleteMany(null!, filter, options, cancellationToken);
        }
        public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNoCollation(options?.Collation);

            return DeleteDocuments(session, FindMatchingIndices(filter));
        }
        public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteMany(filter, cancellationToken));
        }
        public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteMany(filter, options, cancellationToken));
        }
        public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteMany(session, filter, options, cancellationToken));
        }

        #endregion

        #region DeleteOne members

        public DeleteResult DeleteOne(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return DeleteOne(null!, filter, null!, cancellationToken);
        }
        public DeleteResult DeleteOne(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return DeleteOne(null!, filter, options, cancellationToken);
        }
        public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNoCollation(options?.Collation);

            var indices = FindMatchingIndices(filter);
            if (indices.Count > 1)
                indices = [indices[0]];

            return DeleteDocuments(session, indices);
        }
        public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteOne(null!, filter, null!, cancellationToken));
        }
        public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteOne(null!, filter, options, cancellationToken));
        }
        public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteOne(session, filter, options, cancellationToken));
        }

        #endregion

        #region Distinct members

        public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Distinct(null!, field, filter, options, cancellationToken);
        }
        public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(filter);
            EnsureNoCollation(options?.Collation);

            var rendered = field.Render(RenderArgs);
            var valueSerializer = rendered.FieldSerializer ?? BsonSerializer.LookupSerializer<TField>();
            return DistinctValues(valueSerializer, rendered.FieldName, filter);
        }
        public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Distinct(field, filter, options, cancellationToken));
        }
        public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Distinct(session, field, filter, options, cancellationToken));
        }

        #endregion

        #region EstimatedDocumentCount members

        public long EstimatedDocumentCount(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return docs.Count;
        }
        public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EstimatedDocumentCount(options, cancellationToken));
        }

        #endregion

        #region FindAsync members

        public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindSync(null!, filter, options, cancellationToken);
        }
        public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNoCollation(options?.Collation);

            var indices = FindMatchingIndices(filter);
            SortIndices(indices, options?.Sort);

            IEnumerable<int> selected = indices;
            if (options?.Skip != null)
                selected = selected.Skip(options.Skip.Value);
            if (options?.Limit != null)
                selected = selected.Take(options.Limit.Value);

            var results = selected.Select(i => docObjects[i]).OfType<TProjection>().ToList();
            return new FakeAsyncCursor<TProjection>(results);
        }
        public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindAsync(null!, filter, options, cancellationToken);
        }
        public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FindSync(session, filter, options, cancellationToken));
        }

        #endregion

        #region FindOneAndDelete members

        public TProjection FindOneAndDelete<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndDelete(null!, filter, options, cancellationToken);
        }
        public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNoCollation(options?.Collation);

            var indices = FindMatchingIndices(filter);
            if (indices.Count == 0)
                return default!;

            SortIndices(indices, options?.Sort);
            var docIndex = indices[0];

            var document = docObjects[docIndex];
            DeleteDocuments(session, [docIndex]);
            return AsProjection<TProjection>(document);
        }

        public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndDeleteAsync(null!, filter, options, cancellationToken);
        }
        public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FindOneAndDelete(session, filter, options, cancellationToken));
        }

        #endregion

        #region FindOneAndReplace members

        public TProjection FindOneAndReplace<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndReplace(null!, filter, replacement, options, cancellationToken);
        }
        public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNoCollation(options?.Collation);

            var indices = FindMatchingIndices(filter);
            var returnDocument = options?.ReturnDocument ?? ReturnDocument.Before;

            try
            {
                if (indices.Count == 0)
                {
                    if (options?.IsUpsert == true)
                    {
                        InsertDocument(session, replacement);
                        return returnDocument == ReturnDocument.After ? AsProjection<TProjection>(replacement) : default!;
                    }

                    return default!;
                }

                SortIndices(indices, options?.Sort);
                var docIndex = indices[0];

                var before = docObjects[docIndex];
                ReplaceDocumentAt(session, docIndex, replacement);

                return returnDocument == ReturnDocument.After
                    ? AsProjection<TProjection>(replacement)
                    : AsProjection<TProjection>(before);
            }
            catch (MongoWriteException writeException) when (writeException.WriteError != null)
            {
                // findAndModify reports write failures as command errors.
                throw DriverExceptionFactory.CreateCommandException(writeException.WriteError.Code, writeException.WriteError.Message);
            }
        }
        public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndReplaceAsync(null!, filter, replacement, options, cancellationToken);
        }
        public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FindOneAndReplace(session, filter, replacement, options, cancellationToken));
        }

        #endregion

        #region FindOneAndUpdate members

        public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndUpdate(null!, filter, update, options, cancellationToken);
        }
        public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);

            EnsureNoArrayFilters(options?.ArrayFilters);
            EnsureNoCollation(options?.Collation);

            var renderedUpdate = update.Render(RenderArgs);
            if (renderedUpdate is not BsonDocument updateDoc)
                throw new NotSupportedException("Aggregation-pipeline updates are not supported by the in-memory fake.");

            var returnDocument = options?.ReturnDocument ?? ReturnDocument.Before;
            var indices = FindMatchingIndices(filter);

            try
            {
                if (indices.Count == 0)
                {
                    if (options?.IsUpsert == true)
                    {
                        var seed = BsonUpdateEngine.BuildUpsertSeed(RenderFilterForUpsert(filter));
                        ApplyUpdateEngine(updateDoc, seed, insertMode: true);
                        UpsertInsert(session, seed, out var inserted);
                        return returnDocument == ReturnDocument.After ? AsProjection<TProjection>(inserted) : default!;
                    }

                    return default!;
                }

                SortIndices(indices, options?.Sort);
                var docIndex = indices[0];

                var before = docObjects[docIndex];
                UpdateDocumentAt(session, docIndex, updateDoc);
                var after = docObjects[docIndex];

                return returnDocument == ReturnDocument.After
                    ? AsProjection<TProjection>(after)
                    : AsProjection<TProjection>(before);
            }
            catch (MongoWriteException writeException) when (writeException.WriteError != null)
            {
                // findAndModify reports write failures as command errors.
                throw DriverExceptionFactory.CreateCommandException(writeException.WriteError.Code, writeException.WriteError.Message);
            }
        }
        public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndUpdateAsync(null!, filter, update, options, cancellationToken);
        }
        public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FindOneAndUpdate(session, filter, update, options, cancellationToken));
        }

        #endregion

        #region InsertMany members

        public void InsertMany(IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
        {
            InsertMany(null!, documents, options, cancellationToken);
        }
        public void InsertMany(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (documents == null)
                throw new ArgumentNullException(nameof(documents));

            // InsertMany is a bulk insert on a real server: failures surface as
            // MongoBulkWriteException and IsOrdered controls whether the rest is applied.
            var models = documents.Select(it => (WriteModel<TDocument>)new InsertOneModel<TDocument>(it)).ToList();
            if (models.Count == 0)
                throw new ArgumentException("The documents to insert cannot be empty.", nameof(documents));

            BulkWrite(session, models, new BulkWriteOptions { IsOrdered = options?.IsOrdered ?? true }, cancellationToken);
        }
        public Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
        {
            return InsertManyAsync(null!, documents, options, cancellationToken);
        }
        public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
        {
            InsertMany(session, documents, options, cancellationToken);

            return Task.CompletedTask;
        }

        #endregion

        #region InsertOne members

        public void InsertOne(TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
        {
            InsertOne(null!, document, options, cancellationToken);
        }
        public void InsertOne(IClientSessionHandle session, TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
        {
            InsertDocument(session, document);
        }
        public Task InsertOneAsync(TDocument document, CancellationToken _cancellationToken)
        {
            return InsertOneAsync(null!, document, null!, _cancellationToken);
        }
        public Task InsertOneAsync(TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
        {
            return InsertOneAsync(null!, document, options, cancellationToken);
        }
        public Task InsertOneAsync(IClientSessionHandle session, TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
        {
            InsertOne(session, document, options, cancellationToken);

            return Task.CompletedTask;
        }

        #endregion

        #region MapReduce members

        [Obsolete]
        public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("MapReduce is not supported by the in-memory fake.");
        }
        [Obsolete]
        public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("MapReduce is not supported by the in-memory fake.");
        }
        [Obsolete]
        public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("MapReduce is not supported by the in-memory fake.");
        }
        [Obsolete]
        public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("MapReduce is not supported by the in-memory fake.");
        }

        #endregion

        public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : TDocument
        {
            throw new NotSupportedException("OfType filtered collections are not supported by the in-memory fake.");
        }

        #region ReplaceOne members

        public ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOne(null!, filter, replacement, options, cancellationToken);
        }
        public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOneInternal(session, filter, replacement, options?.IsUpsert ?? false, options?.Collation);
        }
        public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOneAsync(null!, filter, replacement, options, cancellationToken);
        }
        public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReplaceOne(session, filter, replacement, options, cancellationToken));
        }

        public ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOne(null!, filter, replacement, options, cancellationToken);
        }
        public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOneInternal(session, filter, replacement, options?.IsUpsert ?? false, options?.Collation);
        }
        public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOneAsync(null!, filter, replacement, options, cancellationToken);
        }
        public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReplaceOne(session, filter, replacement, options, cancellationToken));
        }

        ReplaceOneResult ReplaceOneInternal(IClientSessionHandle? session, FilterDefinition<TDocument> filter, TDocument replacement, bool isUpsert, Collation? collation = null)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            EnsureNoCollation(collation);

            var indices = FindMatchingIndices(filter);
            if (indices.Count == 0)
            {
                if (isUpsert)
                {
                    var upsertedId = InsertDocument(session, replacement);
                    return new ReplaceOneResult.Acknowledged(0, 0, upsertedId);
                }

                return new ReplaceOneResult.Acknowledged(0, 0, null);
            }

            var docIndex = indices[0];
            var modified = ReplaceDocumentAt(session, docIndex, replacement);

            return new ReplaceOneResult.Acknowledged(1, modified ? 1 : 0, null);
        }

        /// <summary>Replaces the document at the given index. Returns true when the stored BSON changed.</summary>
        bool ReplaceDocumentAt(IClientSessionHandle? session, int docIndex, TDocument replacement)
        {
            var currentDoc = docs[docIndex];
            var currentId = GetDocumentIdValue(currentDoc);

            var replacementDoc = SerializeDocument(replacement);

            if (!replacementDoc.Contains("_id"))
                replacementDoc.InsertAt(0, new BsonElement("_id", currentId));

            var replacementId = GetDocumentIdValue(replacementDoc);
            if (replacementId != currentId)
                throw DriverExceptionFactory.CreateImmutableIdException();

            EnsureUniqueIndexes(replacementDoc, docIndex);

            var modified = !replacementDoc.Equals(currentDoc);

            var previousObject = docObjects[docIndex];

            docs[docIndex] = replacementDoc;
            docObjects[docIndex] = replacement;

            if (modified)
            {
                RecordUndo(session, () =>
                {
                    if (!docIds.TryGetValue(currentId, out var currentIndex))
                        return;

                    docs[currentIndex] = currentDoc;
                    docObjects[currentIndex] = previousObject;
                });
            }

            return modified;
        }

        #endregion

        #region UpdateMany members

        public UpdateResult UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return UpdateMany(null!, filter, update, options, cancellationToken);
        }
        public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ApplyUpdate(session, filter, update, options, updateManyDocuments: true);
        }
        public Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return UpdateManyAsync(null!, filter, update, options, cancellationToken);
        }
        public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateMany(session, filter, update, options, cancellationToken));
        }

        #endregion

        #region UpdateOne members

        public UpdateResult UpdateOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return UpdateOne(null!, filter, update, options, cancellationToken);
        }
        public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ApplyUpdate(session, filter, update, options, updateManyDocuments: false);
        }
        public Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return UpdateOneAsync(null!, filter, update, options, cancellationToken);
        }
        public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateOne(session, filter, update, options, cancellationToken));
        }

        #endregion

        #region Watch members

        public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        #endregion

        public IMongoCollection<TDocument> WithReadConcern(ReadConcern readConcern)
        {
            return this;
        }
        public IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference)
        {
            return this;
        }
        public IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            return this;
        }

        public void AggregateToCollection<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        #region DistinctMany members

        public IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            return DistinctMany(null!, field, filter, options, cancellationToken);
        }

        public IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(filter);
            EnsureNoCollation(options?.Collation);

            var rendered = field.Render(RenderArgs);

            IBsonSerializer<TItem> itemSerializer;
            if (rendered.FieldSerializer is IBsonArraySerializer arraySerializer && arraySerializer.TryGetItemSerializationInfo(out var itemInfo) && itemInfo.Serializer is IBsonSerializer<TItem> typedSerializer)
                itemSerializer = typedSerializer;
            else
                itemSerializer = BsonSerializer.LookupSerializer<TItem>();

            return DistinctValues(itemSerializer, rendered.FieldName, filter);
        }

        public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DistinctMany(field, filter, options, cancellationToken));
        }

        public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DistinctMany(session, field, filter, options, cancellationToken));
        }

        #endregion
    }
}
