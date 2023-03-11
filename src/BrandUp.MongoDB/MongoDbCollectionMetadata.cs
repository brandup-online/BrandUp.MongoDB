using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public class MongoDbCollectionMetadata<TDocument> : IMongoDbCollectionMetadata
        where TDocument : class
    {
        public IMongoCollection<TDocument> Collection { get; private set; }

        public MongoDbCollectionMetadata()
        {
            var documentType = DocumentType;

            var collectionAttribute = documentType.GetCustomAttribute<MongoCollectionAttribute>(false) ?? throw new ArgumentException($"Document type {documentType.AssemblyQualifiedName} not contain {nameof(MongoCollectionAttribute)} attribute.");

            string collectionName;
            if (collectionAttribute.CollectionName != null)
                collectionName = collectionAttribute.CollectionName;
            else
                collectionName = TrimCollectionNamePrefix(documentType.Name);

            CollectionName = collectionName;
        }

        #region IMongoDbCollectionContext members

        public string CollectionName { get; }
        public Type DocumentType { get; } = typeof(TDocument);
        void IMongoDbCollectionMetadata.Initialize(MongoDbContext dbContext, CancellationToken cancellationToken)
        {
            var collectionNames = dbContext.Database.ListCollectionNames(cancellationToken: cancellationToken).ToList(cancellationToken);
            if (!collectionNames.Any(name => name.Equals(CollectionName, StringComparison.InvariantCultureIgnoreCase)))
            {
                var createOptions = new CreateCollectionOptions();
                dbContext.Database.CreateCollection(CollectionName, createOptions, cancellationToken);
            }

            var collectionSettings = new MongoCollectionSettings();
            Collection = dbContext.Database.GetCollection<TDocument>(CollectionName, collectionSettings);
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

    public interface IMongoDbCollectionMetadata
    {
        string CollectionName { get; }
        Type DocumentType { get; }
        void Initialize(MongoDbContext dbContext, CancellationToken cancellationToken = default);
    }
}