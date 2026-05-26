using System.Reflection;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Per-document-type registration on a <see cref="MongoDbContext"/>. Holds the collection name
    /// derived from <see cref="MongoCollectionAttribute"/>, the resolved <see cref="IMongoCollection{TDocument}"/>,
    /// and optional configuration hooks applied during context initialization.
    /// </summary>
    public class MongoDbCollectionMetadata<TDocument> : IMongoDbCollectionMetadata
        where TDocument : class
    {
        /// <summary>The resolved collection handle. Available after the owning context has been initialized.</summary>
        public IMongoCollection<TDocument> Collection { get; private set; } = null!;

        /// <summary>
        /// Hook applied to <see cref="CreateCollectionOptions"/> just before the
        /// collection is created (only when the collection does not yet exist).
        /// </summary>
        public Action<CreateCollectionOptions>? ConfigureCreate { get; set; }

        /// <summary>
        /// Hook applied to <see cref="MongoCollectionSettings"/> before the
        /// driver hands out the <see cref="IMongoCollection{TDocument}"/>.
        /// </summary>
        public Action<MongoCollectionSettings>? ConfigureSettings { get; set; }

        internal MongoDbCollectionMetadata()
        {
            var documentType = DocumentType;

            var collectionAttribute = documentType.GetCustomAttribute<MongoCollectionAttribute>(false) ?? throw new ArgumentException($"Document type {documentType.AssemblyQualifiedName} not contain {nameof(MongoCollectionAttribute)} attribute.");

            string collectionName;
            if (collectionAttribute.CollectionName != null)
                collectionName = collectionAttribute.CollectionName;
            else
                collectionName = TrimCollectionNamePrefix(documentType.Name);

            Name = collectionName;
        }

        #region IMongoDbCollectionContext members

        public string Name { get; }
        public Type DocumentType { get; } = typeof(TDocument);
        void IMongoDbCollectionMetadata.Initialize(MongoDbContext dbContext, CancellationToken cancellationToken)
        {
            var collectionNames = dbContext.Database.ListCollectionNames(cancellationToken: cancellationToken).ToList(cancellationToken);
            if (!collectionNames.Any(name => name.Equals(Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                var createOptions = new CreateCollectionOptions();
                ConfigureCreate?.Invoke(createOptions);
                dbContext.Database.CreateCollection(Name, createOptions, cancellationToken);
            }

            var collectionSettings = new MongoCollectionSettings();
            ConfigureSettings?.Invoke(collectionSettings);
            Collection = dbContext.Database.GetCollection<TDocument>(Name, collectionSettings);
        }

        #endregion

        public static string TrimCollectionNamePrefix(string name)
        {
            if (name.EndsWith("Document"))
                return name[..^"Document".Length];
            else if (name.EndsWith("Model"))
                return name[..^"Model".Length];
            return name;
        }
    }

    /// <summary>Untyped collection registration on a <see cref="MongoDbContext"/>.</summary>
    public interface IMongoDbCollectionMetadata
    {
        /// <summary>Collection name in the MongoDB database.</summary>
        string Name { get; }

        /// <summary>The .NET type of the documents stored in this collection.</summary>
        Type DocumentType { get; }

        /// <summary>Resolves the underlying <see cref="IMongoCollection{TDocument}"/> handle. Invoked once by the owning context.</summary>
        void Initialize(MongoDbContext dbContext, CancellationToken cancellationToken = default);
    }
}