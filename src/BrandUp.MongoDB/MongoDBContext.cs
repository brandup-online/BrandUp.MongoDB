using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Base class for a strongly-typed MongoDB database context. Derive a class, declare
    /// <see cref="IMongoCollection{TDocument}"/> properties for each collection, and register
    /// the context with <c>services.AddMongoDbContext&lt;TContext&gt;(...)</c>.
    /// </summary>
    public abstract class MongoDbContext
    {
        MongoDbContextOptions options = null!;
        readonly List<IMongoDbCollectionMetadata> collections = [];
        readonly Dictionary<Type, int> collectionTypes = [];
        readonly Dictionary<string, int> collectionNames = [];

        /// <summary>The shared <see cref="IMongoClient"/> resolved from the registered <see cref="IMongoDbClientFactory"/>.</summary>
        public IMongoClient Client { get; private set; } = null!;

        /// <summary>The database handle for the database named in <see cref="MongoDbContextOptions.DatabaseName"/>.</summary>
        public IMongoDatabase Database { get; private set; } = null!;

        /// <summary>The collection metadata discovered on this context (one entry per registered document type).</summary>
        public IEnumerable<IMongoDbCollectionMetadata> Collections => collections;

        #region Methods

        internal void Initialize(IServiceProvider serviceProvider, List<IMongoDbCollectionMetadata> collections)
        {
            var mongoClientFactory = serviceProvider.GetRequiredService<IMongoDbClientFactory>();

            var optionsFactory = serviceProvider.GetRequiredService<IOptionsFactory<MongoDbContextOptions>>();

            var optionsName = GetType().FullName!;
            options = optionsFactory.Create(optionsName);

            Client = mongoClientFactory.ResolveClient();
            Database = Client.GetDatabase(options.DatabaseName, options.DatabaseSettings);

            var i = 0;
            foreach (var collection in collections)
            {
                collection.Initialize(this, options.UpdateExistingCollections);

                this.collections.Add(collection);
                collectionTypes.Add(collection.DocumentType, i);
                collectionNames.Add(collection.Name.ToLowerInvariant(), i);

                i++;
            }
        }

        internal bool TryGetCollectionContext(Type documentType, [MaybeNullWhen(false)] out IMongoDbCollectionMetadata collectionContext)
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

        internal bool TryGetCollectionContext(string collectionName, [MaybeNullWhen(false)] out IMongoDbCollectionMetadata collectionContext)
        {
            ArgumentNullException.ThrowIfNull(collectionName);

            if (!collectionNames.TryGetValue(collectionName.ToLowerInvariant(), out int index))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = collections[index];
            return true;
        }

        /// <summary>
        /// Attempts to resolve the <see cref="MongoDbCollectionMetadata{TDocument}"/> for the given document type.
        /// </summary>
        public bool TryGetCollectionContext<TDocument>([MaybeNullWhen(false)] out MongoDbCollectionMetadata<TDocument> collectionContext)
            where TDocument : class
        {
            if (!TryGetCollectionContext(typeof(TDocument), out var collectionMetadata2))
            {
                collectionContext = null;
                return false;
            }

            collectionContext = (MongoDbCollectionMetadata<TDocument>)collectionMetadata2;
            return true;
        }

        /// <summary>
        /// Resolves the <see cref="MongoDbCollectionMetadata{TDocument}"/> for the given document type,
        /// throwing if the document type was not registered on this context.
        /// </summary>
        /// <exception cref="ArgumentException">No collection for <typeparamref name="TDocument"/> is registered on this context.</exception>
        public MongoDbCollectionMetadata<TDocument> GetCollectionContext<TDocument>()
            where TDocument : class
        {
            if (!TryGetCollectionContext(out MongoDbCollectionMetadata<TDocument>? collectionContext))
                throw new ArgumentException($"Not found collection context of document type \"{typeof(TDocument).AssemblyQualifiedName}\" by \"{GetType().AssemblyQualifiedName}\".");

            return collectionContext;
        }

        /// <summary>Returns the <see cref="IMongoCollection{TDocument}"/> for the registered document type.</summary>
        /// <exception cref="ArgumentException">No collection for <typeparamref name="TDocument"/> is registered on this context.</exception>
        public IMongoCollection<TDocument> GetCollection<TDocument>()
            where TDocument : class
        {
            var collectionContext = GetCollectionContext<TDocument>();
            return collectionContext.Collection;
        }

        #endregion
    }
}
