using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace BrandUp.MongoDB
{
    public abstract class MongoDbContext : IDisposable
    {
        private readonly List<IMongoDbCollectionContext> collections = new List<IMongoDbCollectionContext>();
        private readonly Dictionary<Type, int> collectionDocumentTypes = new Dictionary<Type, int>();
        private readonly Dictionary<string, int> collectionNames = new Dictionary<string, int>();

        public IMongoDatabase Database { get; }

        public MongoDbContext(MongoDbContextOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.ClientFactory == null)
                throw new ArgumentNullException(nameof(options.ClientFactory));

            var client = options.ClientFactory.CreateClient(options.Url);
            Database = client.GetDatabase(options.Url.DatabaseName);

            var contextType = GetType();

            foreach (var collectionOptions in options.Collections)
            {
                var collectionContext = collectionOptions.BuildContext();

                collectionContext.Initialize(collectionOptions.CollectionName, this);

                var index = collections.Count;
                collections.Add(collectionContext);
                collectionDocumentTypes.Add(collectionContext.DocumentType, index);
                collectionNames.Add(collectionContext.CollectionName.ToLower(), index);
            }
        }

        #region Methods

        internal bool TryGetCollectionContext(Type documentType, out IMongoDbCollectionContext collectionContext)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            if (!collectionDocumentTypes.TryGetValue(documentType, out int index))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = collections[index];
            return true;
        }
        internal bool TryGetCollectionContext(string collectionName, out IMongoDbCollectionContext collectionContext)
        {
            if (collectionName == null)
                throw new ArgumentNullException(nameof(collectionName));

            if (!collectionNames.TryGetValue(collectionName.ToLower(), out int index))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = collections[index];
            return true;
        }
        public bool TryGetCollectionContext<TDocument>(out MongoDbCollectionContext<TDocument> collectionContext)
            where TDocument : class
        {
            if (!TryGetCollectionContext(typeof(TDocument), out IMongoDbCollectionContext collectionMetadata2))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = (MongoDbCollectionContext<TDocument>)collectionMetadata2;
            return true;
        }
        public MongoDbCollectionContext<TDocument> GetCollectionContext<TDocument>()
            where TDocument : class
        {
            if (!TryGetCollectionContext(out MongoDbCollectionContext<TDocument> collectionContext))
                throw new ArgumentException();

            return collectionContext;
        }
        public IMongoCollection<TDocument> GetCollection<TDocument>()
            where TDocument : class
        {
            if (!TryGetCollectionContext(out MongoDbCollectionContext<TDocument> collectionContext))
                throw new ArgumentException();

            return collectionContext.Collection;
        }

        #endregion

        #region IDisposable members

        public void Dispose()
        {
        }

        #endregion
    }
}