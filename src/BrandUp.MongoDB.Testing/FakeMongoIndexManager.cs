using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoIndexManager<TDocument> : IMongoIndexManager<TDocument>
    {
        readonly FakeMongoCollection<TDocument> collection;
        readonly Dictionary<string, BsonDocument> indexes = new Dictionary<string, BsonDocument>();

        public FakeMongoIndexManager(FakeMongoCollection<TDocument> collection)
        {
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public CollectionNamespace CollectionNamespace => collection.CollectionNamespace;
        public IBsonSerializer<TDocument> DocumentSerializer => collection.DocumentSerializer;
        public MongoCollectionSettings Settings => collection.Settings;

        private string Insert(CreateIndexModel<TDocument> indexModel)
        {
            if (indexModel == null)
                throw new ArgumentNullException(nameof(indexModel));

            var name = indexModel.Options?.Name;
            if (name == null)
                name = $"index{indexes.Count}";

            indexes.Add(name.ToLower(), new BsonDocument { new BsonElement("name", name) });

            return name;
        }

        public IEnumerable<string> CreateMany(IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default)
        {
            return CreateMany(null, models, null, cancellationToken);
        }
        public IEnumerable<string> CreateMany(IEnumerable<CreateIndexModel<TDocument>> models, CreateManyIndexesOptions options, CancellationToken cancellationToken = default)
        {
            return CreateMany(null, models, options, cancellationToken);
        }
        public IEnumerable<string> CreateMany(IClientSessionHandle session, IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default)
        {
            return CreateMany(session, null, cancellationToken);
        }
        public IEnumerable<string> CreateMany(IClientSessionHandle session, IEnumerable<CreateIndexModel<TDocument>> models, CreateManyIndexesOptions options, CancellationToken cancellationToken = default)
        {
            var names = new List<string>();

            foreach (var model in models)
                names.Add(Insert(model));

            return names;
        }

        public Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateMany(models, cancellationToken));
        }
        public Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateIndexModel<TDocument>> models, CreateManyIndexesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateMany(models, options, cancellationToken));
        }
        public Task<IEnumerable<string>> CreateManyAsync(IClientSessionHandle session, IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateMany(session, models, cancellationToken));
        }
        public Task<IEnumerable<string>> CreateManyAsync(IClientSessionHandle session, IEnumerable<CreateIndexModel<TDocument>> models, CreateManyIndexesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateMany(session, models, options, cancellationToken));
        }

        public string CreateOne(CreateIndexModel<TDocument> model, CreateOneIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return CreateOne(null, model, options, cancellationToken);
        }
        public string CreateOne(IndexKeysDefinition<TDocument> keys, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return CreateOne(null, keys, options, cancellationToken);
        }
        public string CreateOne(IClientSessionHandle session, IndexKeysDefinition<TDocument> keys, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return CreateOne(session, new CreateIndexModel<TDocument>(keys, options), null, cancellationToken);
        }
        public string CreateOne(IClientSessionHandle session, CreateIndexModel<TDocument> model, CreateOneIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Insert(model);
        }

        public Task<string> CreateOneAsync(CreateIndexModel<TDocument> model, CreateOneIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateOne(model, options, cancellationToken));
        }
        public Task<string> CreateOneAsync(IndexKeysDefinition<TDocument> keys, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateOne(keys, options, cancellationToken));
        }
        public Task<string> CreateOneAsync(IClientSessionHandle session, IndexKeysDefinition<TDocument> keys, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateOne(session, keys, options, cancellationToken));
        }
        public Task<string> CreateOneAsync(IClientSessionHandle session, CreateIndexModel<TDocument> model, CreateOneIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateOne(session, model, options, cancellationToken));
        }

        public void DropAll(DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            DropAll(null, options, cancellationToken);
        }
        public void DropAll(CancellationToken cancellationToken = default)
        {
            DropAll(null, null, cancellationToken);
        }
        public void DropAll(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            DropAll(session, null, cancellationToken);
        }
        public void DropAll(IClientSessionHandle session, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            indexes.Clear();
        }

        public Task DropAllAsync(DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            return DropAllAsync(null, options, cancellationToken);
        }
        public Task DropAllAsync(CancellationToken cancellationToken = default)
        {
            return DropAllAsync(null, null, cancellationToken);
        }
        public Task DropAllAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return DropAllAsync(session, null, cancellationToken);
        }
        public Task DropAllAsync(IClientSessionHandle session, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            DropAll(session, options, cancellationToken);

            return Task.CompletedTask;
        }

        public void DropOne(string name, CancellationToken cancellationToken = default)
        {
            DropOne(null, name, null, cancellationToken);
        }
        public void DropOne(string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            DropOne(null, name, options, cancellationToken);
        }
        public void DropOne(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            DropOne(session, name, null, cancellationToken);
        }
        public void DropOne(IClientSessionHandle session, string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            if (!indexes.Remove(name))
                throw new InvalidOperationException();
        }

        public Task DropOneAsync(string name, CancellationToken cancellationToken = default)
        {
            return DropOneAsync(null, name, null, cancellationToken);
        }
        public Task DropOneAsync(string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            return DropOneAsync(null, name, options, cancellationToken);
        }
        public Task DropOneAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            return DropOneAsync(session, name, null, cancellationToken);
        }
        public Task DropOneAsync(IClientSessionHandle session, string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            DropOne(session, name, options, cancellationToken);

            return Task.CompletedTask;
        }

        public IAsyncCursor<BsonDocument> List(CancellationToken cancellationToken = default)
        {
            return List((IClientSessionHandle)null, cancellationToken);
        }
        public IAsyncCursor<BsonDocument> List(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<BsonDocument>(indexes.Values);
        }
        public Task<IAsyncCursor<BsonDocument>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(List(cancellationToken));
        }
        public Task<IAsyncCursor<BsonDocument>> ListAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(List(session, cancellationToken));
        }

        public IAsyncCursor<BsonDocument> List(ListIndexesOptions options, CancellationToken cancellationToken = default)
        {
            return List(null, options, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> List(IClientSessionHandle session, ListIndexesOptions options = null, CancellationToken cancellationToken = default)
        {
            return new FakeAsyncCursor<BsonDocument>(indexes.Values);
        }

        public Task<IAsyncCursor<BsonDocument>> ListAsync(ListIndexesOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(List(options, cancellationToken));
        }

        public Task<IAsyncCursor<BsonDocument>> ListAsync(IClientSessionHandle session, ListIndexesOptions options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(List(session, options, cancellationToken));
        }
    }
}