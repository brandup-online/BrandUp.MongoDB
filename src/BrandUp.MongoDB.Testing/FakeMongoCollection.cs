using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoCollection<TDocument> : IMongoCollection<TDocument>, IFakeMongoCollection
    {
        readonly FakeMongoDatabase database;
        readonly List<TDocument> documents = new List<TDocument>();
        readonly Dictionary<string, int> ids = new Dictionary<string, int>();
        readonly FakeMongoIndexManager<TDocument> indexManager;

        public FakeMongoCollection(FakeMongoDatabase database, string name, MongoCollectionSettings settings)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CollectionNamespace = new CollectionNamespace(database.DatabaseNamespace, name);
            DocumentSerializer = settings.SerializerRegistry.GetSerializer<TDocument>();
            indexManager = new FakeMongoIndexManager<TDocument>(this);
        }

        public CollectionNamespace CollectionNamespace { get; }
        public IMongoDatabase Database => database;
        public IBsonSerializer<TDocument> DocumentSerializer { get; }
        public IMongoIndexManager<TDocument> Indexes => indexManager;
        public MongoCollectionSettings Settings { get; }

        #region Aggregate members

        public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region BulkWrite members

        public BulkWriteResult<TDocument> BulkWrite(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public BulkWriteResult<TDocument> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<BulkWriteResult<TDocument>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Count methods

        public long Count(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return Count(null, filter, options, cancellationToken);
        }

        public long Count(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return CountDocuments(session, filter, options, cancellationToken);
        }

        public Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return CountAsync(null, filter, options, cancellationToken);
        }

        public Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Count(session, filter, options, cancellationToken));
        }

        #endregion

        #region CountDocuments methods

        public long CountDocuments(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return CountDocuments(null, filter, options, cancellationToken);
        }
        public long CountDocuments(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            var ef = filter as ExpressionFilterDefinition<TDocument> ?? throw new InvalidCastException();
            return documents.Count(ef.Expression.Compile());
        }
        public Task<long> CountDocumentsAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return CountDocumentsAsync(null, filter, options, cancellationToken);
        }
        public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CountDocuments(session, filter, options, cancellationToken));
        }

        #endregion

        #region DeleteMany members

        public DeleteResult DeleteMany(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return DeleteMany(null, filter, null, cancellationToken);
        }
        public DeleteResult DeleteMany(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return DeleteMany(null, filter, options, cancellationToken);
        }
        public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default)
        {
            var ef = filter as ExpressionFilterDefinition<TDocument> ?? throw new InvalidCastException();

            long deletedCound = 0;
            var docs = documents.Where(ef.Expression.Compile()).ToList();
            foreach (var d in docs)
            {
                if (documents.Remove(d))
                    deletedCound++;
            }

            if (deletedCound > 0)
                return new DeleteResult.Acknowledged(deletedCound);
            return DeleteResult.Unacknowledged.Instance;
        }
        public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteMany(filter, cancellationToken));
        }
        public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteMany(filter, options, cancellationToken));
        }
        public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteMany(session, filter, options, cancellationToken));
        }

        #endregion

        #region DeleteOne members

        public DeleteResult DeleteOne(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return DeleteOne(null, filter, null, cancellationToken);
        }
        public DeleteResult DeleteOne(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return DeleteOne(null, filter, options, cancellationToken);
        }
        public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default)
        {
            var ef = filter as ExpressionFilterDefinition<TDocument> ?? throw new InvalidCastException();

            long deletedCound = 0;
            var docs = documents.Where(ef.Expression.Compile()).ToList();
            if (docs.Count > 1)
                throw new InvalidOperationException();
            foreach (var d in docs)
            {
                if (documents.Remove(d))
                    deletedCound++;
            }

            if (deletedCound > 0)
                return new DeleteResult.Acknowledged(deletedCound);
            return DeleteResult.Unacknowledged.Instance;
        }
        public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteOne(null, filter, null, cancellationToken));
        }
        public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteOne(null, filter, options, cancellationToken));
        }
        public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteOne(session, filter, options, cancellationToken));
        }

        #endregion

        #region Distinct members

        public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region EstimatedDocumentCount members

        public long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default)
        {
            return documents.Count;
        }
        public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EstimatedDocumentCount(options, cancellationToken));
        }

        #endregion

        #region FindAsync members

        public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            return FindSync(null, filter, options, cancellationToken);
        }
        public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            var ef = filter as ExpressionFilterDefinition<TDocument> ?? throw new InvalidCastException();
            var docs = documents.Where(ef.Expression.Compile()).OfType<TProjection>().ToList();
            return new FakeAsyncCursor<TProjection>(docs);
        }
        public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            return FindAsync(null, filter, options, cancellationToken);
        }
        public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FindSync(session, filter, options, cancellationToken));
        }

        #endregion

        #region FindOneAndDelete members

        public TProjection FindOneAndDelete<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            var filded = FindSync<TDocument>(session, filter, null, cancellationToken);
            var doc = filded.SingleOrDefault(cancellationToken);
            if (documents.Remove(doc))
                throw new Exception();
            return (TProjection)(object)doc;
        }
        public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            return FindOneAndDeleteAsync(null, filter, options, cancellationToken);
        }
        public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FindOneAndDelete(session, filter, options, cancellationToken));
        }

        #endregion

        #region FindOneAndReplace members

        public TProjection FindOneAndReplace<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region FindOneAndUpdate members

        public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region InsertMany members

        public void InsertMany(IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
        {
            InsertMany(null, documents, options, cancellationToken);
        }
        public void InsertMany(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
        {
            this.documents.AddRange(documents);
        }
        public Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
        {
            return InsertManyAsync(null, documents, options, cancellationToken);
        }
        public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
        {
            InsertMany(session, documents, options, cancellationToken);

            return Task.CompletedTask;
        }

        #endregion

        #region InsertOne members

        public void InsertOne(TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
        {
            InsertOne(null, document, options, cancellationToken);
        }
        public void InsertOne(IClientSessionHandle session, TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
        {
            documents.Add(document);
        }
        public Task InsertOneAsync(TDocument document, CancellationToken _cancellationToken)
        {
            return InsertOneAsync(null, document, null, _cancellationToken);
        }
        public Task InsertOneAsync(TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
        {
            return InsertOneAsync(null, document, options, cancellationToken);
        }
        public Task InsertOneAsync(IClientSessionHandle session, TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
        {
            InsertOne(session, document, options, cancellationToken);

            return Task.CompletedTask;
        }

        #endregion

        #region MapReduce members

        public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : TDocument
        {
            throw new System.NotImplementedException();
        }

        #region ReplaceOne members

        public ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOne(null, filter, replacement, options, cancellationToken);
        }
        public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            var ef = filter as ExpressionFilterDefinition<TDocument> ?? throw new InvalidCastException();
            var doc = documents.Where(ef.Expression.Compile()).FirstOrDefault();
            var index = documents.IndexOf(doc);
            if (index != -1)
            {
                documents[index] = replacement;
                return new ReplaceOneResult.Acknowledged(1, 1, BsonValue.Create(0));
            }
            return ReplaceOneResult.Unacknowledged.Instance;
        }
        public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            return ReplaceOneAsync(null, filter, replacement, options, cancellationToken);
        }
        public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReplaceOne(session, filter, replacement, options, cancellationToken));
        }

        #endregion

        #region UpdateMany members

        public UpdateResult UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region UpdateOne members

        public UpdateResult UpdateOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Watch members

        public IAsyncCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public IAsyncCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        public Task<IAsyncCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
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
    }
}