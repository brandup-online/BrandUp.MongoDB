using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoClient : IMongoClient
    {
        readonly Dictionary<string, FakeMongoDatabase> databases = new();
        readonly FakeCluster cluster = new();

        public FakeMongoClient(string connectionString)
        {
            Settings = MongoClientSettings.FromConnectionString(connectionString);
        }

        #region IMongoClient members

        public ICluster Cluster => cluster;
        public MongoClientSettings Settings { get; }

        public ClientBulkWriteResult BulkWrite(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Client-level bulk write across multiple namespaces is not supported by the in-memory fake.");
        }

        public ClientBulkWriteResult BulkWrite(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Client-level bulk write across multiple namespaces is not supported by the in-memory fake.");
        }

        public Task<ClientBulkWriteResult> BulkWriteAsync(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Client-level bulk write across multiple namespaces is not supported by the in-memory fake.");
        }

        public Task<ClientBulkWriteResult> BulkWriteAsync(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Client-level bulk write across multiple namespaces is not supported by the in-memory fake.");
        }

        public void Dispose()
        {
        }

        public void DropDatabase(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(name);

            databases.Remove(name.ToLower());
        }

        public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            DropDatabase(name, cancellationToken);
        }

        public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            DropDatabase(name, cancellationToken);

            return Task.CompletedTask;
        }

        public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            DropDatabase(session, name, cancellationToken);

            return Task.CompletedTask;
        }

        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings? settings = null)
        {
            if (!databases.TryGetValue(name.ToLower(), out FakeMongoDatabase? database))
                databases.Add(name.ToLower(), database = new FakeMongoDatabase(this, name, settings ?? new MongoDatabaseSettings()));
            return database;
        }

        public IAsyncCursor<string> ListDatabaseNames(CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<string>(DatabaseNames());
        }

        public IAsyncCursor<string> ListDatabaseNames(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<string>(DatabaseNames());
        }

        public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<string>(DatabaseNames());
        }

        public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<string>(DatabaseNames());
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabaseNames(cancellationToken));
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabaseNames(options, cancellationToken));
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabaseNames(session, cancellationToken));
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabaseNames(session, options, cancellationToken));
        }

        public IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<BsonDocument>(DatabaseDescriptions());
        }

        public IAsyncCursor<BsonDocument> ListDatabases(ListDatabasesOptions options, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<BsonDocument>(DatabaseDescriptions());
        }

        public IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<BsonDocument>(DatabaseDescriptions());
        }

        public IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<BsonDocument>(DatabaseDescriptions());
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabases(cancellationToken));
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(ListDatabasesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabases(options, cancellationToken));
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabases(session, cancellationToken));
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ListDatabases(session, options, cancellationToken));
        }

        public IClientSessionHandle StartSession(ClientSessionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new FakeClientSessionHandle(this);
        }

        public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(StartSession(options, cancellationToken));
        }

        public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Change streams are not supported by the in-memory fake.");
        }

        public IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            return this;
        }

        public IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            return this;
        }

        public IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            return this;
        }

        #endregion

        #region Helpers

        IEnumerable<string> DatabaseNames()
        {
            return databases.Values.Select(database => database.DatabaseNamespace.DatabaseName).ToArray();
        }

        IEnumerable<BsonDocument> DatabaseDescriptions()
        {
            return databases.Values
                .Select(database => new BsonDocument
                {
                    { "name", database.DatabaseNamespace.DatabaseName },
                    { "sizeOnDisk", 0L },
                    { "empty", true }
                })
                .ToArray();
        }

        #endregion
    }
}