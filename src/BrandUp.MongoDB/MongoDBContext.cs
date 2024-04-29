using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public abstract class MongoDbContext : IDisposable
    {
        MongoDbContextOptions options;
        readonly List<IMongoDbCollectionMetadata> collections = [];
        readonly Dictionary<Type, int> collectionTypes = [];
        readonly Dictionary<string, int> collectionNames = [];

        public IMongoDatabase Database { get; private set; }
        public IEnumerable<IMongoDbCollectionMetadata> Collections => collections;

        #region Methods

        internal void Initialize(IServiceProvider serviceProvider, List<IMongoDbCollectionMetadata> collections)
        {
            var mongoClientFactory = serviceProvider.GetRequiredService<IMongoDbClientFactory>();

            var optionsFactory = serviceProvider.GetRequiredService<IOptionsFactory<MongoDbContextOptions>>();

            var optionsName = GetType().FullName;
            options = optionsFactory.Create(optionsName);

            var mongoClient = mongoClientFactory.ResolveClient(options.ConnectionString);
            Database = mongoClient.GetDatabase(options.DatabaseName, new MongoDatabaseSettings());

            var i = 0;
            foreach (var collection in collections)
            {
                collection.Initialize(this);

                this.collections.Add(collection);
                collectionTypes.Add(collection.DocumentType, i);
                collectionNames.Add(collection.CollectionName, i);

                i++;
            }
        }
        internal bool TryGetCollectionContext(Type documentType, out IMongoDbCollectionMetadata collectionContext)
        {
            ArgumentNullException.ThrowIfNull(documentType);

            if (!collectionTypes.TryGetValue(documentType, out int index))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = collections[index];
            return true;
        }
        internal bool TryGetCollectionContext(string collectionName, out IMongoDbCollectionMetadata collectionContext)
        {
            ArgumentNullException.ThrowIfNull(collectionName);

            if (!collectionNames.TryGetValue(collectionName.ToLower(), out int index))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = collections[index];
            return true;
        }
        public bool TryGetCollectionContext<TDocument>(out MongoDbCollectionMetadata<TDocument> collectionContext)
            where TDocument : class
        {
            if (!TryGetCollectionContext(typeof(TDocument), out IMongoDbCollectionMetadata collectionMetadata2))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = (MongoDbCollectionMetadata<TDocument>)collectionMetadata2;
            return true;
        }
        public MongoDbCollectionMetadata<TDocument> GetCollectionContext<TDocument>()
            where TDocument : class
        {
            if (!TryGetCollectionContext(out MongoDbCollectionMetadata<TDocument> collectionContext))
                throw new ArgumentException();

            return collectionContext;
        }
        public IMongoCollection<TDocument> GetCollection<TDocument>()
            where TDocument : class
        {
            if (!TryGetCollectionContext(out MongoDbCollectionMetadata<TDocument> collectionContext))
                throw new ArgumentException();

            return collectionContext.Collection;
        }

        #endregion

        #region IDisposable members

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
                disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}