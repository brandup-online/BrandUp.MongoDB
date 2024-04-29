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
                dbContext.Database.CreateCollection(Name, createOptions, cancellationToken);
            }

            var collectionSettings = new MongoCollectionSettings();
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

    public interface IMongoDbCollectionMetadata
    {
        string Name { get; }
        Type DocumentType { get; }
        void Initialize(MongoDbContext dbContext, CancellationToken cancellationToken = default);
    }
}