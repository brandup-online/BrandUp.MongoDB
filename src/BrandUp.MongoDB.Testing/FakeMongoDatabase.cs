using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoDatabase : IMongoDatabase
    {
        private readonly Dictionary<string, IFakeMongoCollection> collections = new Dictionary<string, IFakeMongoCollection>();
        // Names created via CreateCollection that have not yet been materialized as a typed
        // FakeMongoCollection<TDocument> by GetCollection. Kept separate because CreateCollection
        // has no document type to instantiate a typed collection with.
        private readonly HashSet<string> declaredCollections = new HashSet<string>();

        public FakeMongoDatabase(IMongoClient client, string name, MongoDatabaseSettings settings)
        {
            Client = client;
            DatabaseNamespace = new DatabaseNamespace(name);
            Settings = settings ?? new MongoDatabaseSettings();
        }

        public IMongoClient Client { get; }

        public DatabaseNamespace DatabaseNamespace { get; }

        public MongoDatabaseSettings Settings { get; }

        public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public void AggregateToCollection<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Aggregation pipelines are not supported by the in-memory fake.");
        }

        public void CreateCollection(string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(name);

            var key = name.ToLower();
            if (collections.ContainsKey(key) || !declaredCollections.Add(key))
                throw new InvalidOperationException($"Collection \"{name}\" already exists.");
        }

        public void CreateCollection(IClientSessionHandle session, string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            CreateCollection(name, options, cancellationToken);
        }

        public Task CreateCollectionAsync(string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            CreateCollection(name, options, cancellationToken);

            return Task.CompletedTask;
        }

        public Task CreateCollectionAsync(IClientSessionHandle session, string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            CreateCollection(session, name, options, cancellationToken);

            return Task.CompletedTask;
        }

        public void CreateView<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Views are not supported by the in-memory fake.");
        }

        public void CreateView<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Views are not supported by the in-memory fake.");
        }

        public Task CreateViewAsync<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Views are not supported by the in-memory fake.");
        }

        public Task CreateViewAsync<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Views are not supported by the in-memory fake.");
        }

        public void DropCollection(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(name);

            var key = name.ToLower();
            collections.Remove(key);
            declaredCollections.Remove(key);
        }

        public void DropCollection(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            DropCollection(name, cancellationToken);
        }

        public void DropCollection(string name, DropCollectionOptions options, CancellationToken cancellationToken = default)
        {
            DropCollection(name, cancellationToken);
        }

        public void DropCollection(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken = default)
        {
            DropCollection(name, cancellationToken);
        }

        public Task DropCollectionAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            DropCollection(name, cancellationToken);

            return Task.CompletedTask;
        }

        public Task DropCollectionAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            DropCollection(session, name, cancellationToken);

            return Task.CompletedTask;
        }

        public Task DropCollectionAsync(string name, DropCollectionOptions options, CancellationToken cancellationToken = default)
        {
            DropCollection(name, options, cancellationToken);

            return Task.CompletedTask;
        }

        public Task DropCollectionAsync(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken = default)
        {
            DropCollection(session, name, options, cancellationToken);

            return Task.CompletedTask;
        }

        public IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings? settings = null)
        {
            var key = name.ToLower();
            if (!collections.TryGetValue(key, out IFakeMongoCollection? fakeMongoCollection))
            {
                collections.Add(key, fakeMongoCollection = new FakeMongoCollection<TDocument>(this, name, settings ?? new MongoCollectionSettings()));
                declaredCollections.Remove(key);
            }

            return (IMongoCollection<TDocument>)fakeMongoCollection;
        }

        public IAsyncCursor<string> ListCollectionNames(ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new FakeAsyncCursor<string>(CollectionNames());
        }

        public IAsyncCursor<string> ListCollectionNames(IClientSessionHandle session, ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ListCollectionNames(options, cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListCollectionNamesAsync(ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ListCollectionNames(options, cancellationToken));
        }

        public Task<IAsyncCursor<string>> ListCollectionNamesAsync(IClientSessionHandle session, ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ListCollectionNames(session, options, cancellationToken));
        }

        public IAsyncCursor<BsonDocument> ListCollections(ListCollectionsOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new FakeAsyncCursor<BsonDocument>(CollectionDescriptions());
        }

        public IAsyncCursor<BsonDocument> ListCollections(IClientSessionHandle session, ListCollectionsOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new FakeAsyncCursor<BsonDocument>(CollectionDescriptions());
        }

        public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ListCollections(options, cancellationToken));
        }

        public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(IClientSessionHandle session, ListCollectionsOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ListCollections(session, options, cancellationToken));
        }

        public void RenameCollection(string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(oldName);
            ArgumentNullException.ThrowIfNull(newName);

            var oldKey = oldName.ToLower();
            var newKey = newName.ToLower();

            var sourceMaterialized = collections.TryGetValue(oldKey, out var collection);
            if (!sourceMaterialized && !declaredCollections.Contains(oldKey))
                throw new InvalidOperationException($"Source collection \"{oldName}\" does not exist.");

            var targetExists = collections.ContainsKey(newKey) || declaredCollections.Contains(newKey);
            if (targetExists && !(options?.DropTarget ?? false))
                throw new InvalidOperationException($"Target collection \"{newName}\" already exists.");

            collections.Remove(newKey);
            declaredCollections.Remove(newKey);

            if (sourceMaterialized)
            {
                collections.Remove(oldKey);
                collections[newKey] = collection!;
            }
            else
            {
                declaredCollections.Remove(oldKey);
                declaredCollections.Add(newKey);
            }
        }

        public void RenameCollection(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            RenameCollection(oldName, newName, options, cancellationToken);
        }

        public Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            RenameCollection(oldName, newName, options, cancellationToken);

            return Task.CompletedTask;
        }

        public Task RenameCollectionAsync(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            RenameCollection(session, oldName, newName, options, cancellationToken);

            return Task.CompletedTask;
        }

        public TResult RunCommand<TResult>(Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Running raw database commands is not supported by the in-memory fake.");
        }

        public TResult RunCommand<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Running raw database commands is not supported by the in-memory fake.");
        }

        public Task<TResult> RunCommandAsync<TResult>(Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Running raw database commands is not supported by the in-memory fake.");
        }

        public Task<TResult> RunCommandAsync<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException("Running raw database commands is not supported by the in-memory fake.");
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

        public IMongoDatabase WithReadConcern(ReadConcern readConcern)
        {
            return this;
        }

        public IMongoDatabase WithReadPreference(ReadPreference readPreference)
        {
            return this;
        }

        public IMongoDatabase WithWriteConcern(WriteConcern writeConcern)
        {
            return this;
        }

        string[] CollectionNames()
        {
            return collections.Keys.Concat(declaredCollections).ToArray();
        }

        IEnumerable<BsonDocument> CollectionDescriptions()
        {
            return CollectionNames()
                .Select(name => new BsonDocument
                {
                    { "name", name },
                    { "type", "collection" }
                })
                .ToArray();
        }
    }

    internal interface IFakeMongoCollection
    {
        CollectionNamespace CollectionNamespace { get; }
        IMongoDatabase Database { get; }
        MongoCollectionSettings Settings { get; }
    }
}