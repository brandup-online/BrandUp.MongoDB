using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            return Array.Empty<string>();
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
            return null;
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
            throw new NotImplementedException();
        }
        public void DropAll(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public void DropAll(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public void DropAll(IClientSessionHandle session, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DropAllAsync(DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task DropAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task DropAllAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task DropAllAsync(IClientSessionHandle session, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void DropOne(string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void DropOne(string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void DropOne(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void DropOne(IClientSessionHandle session, string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DropOneAsync(string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DropOneAsync(string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DropOneAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DropOneAsync(IClientSessionHandle session, string name, DropIndexOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncCursor<BsonDocument> List(CancellationToken cancellationToken = default)
        {
            return List(null, cancellationToken);
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
    }
}