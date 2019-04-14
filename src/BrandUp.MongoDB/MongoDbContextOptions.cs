using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace BrandUp.MongoDB
{
    public class MongoDbContextOptions
    {
        public IMongoDbClientFactory ClientFactory { get; set; }
        public MongoUrl Url { get; set; }
        public List<MongoDbCollectionOptions> Collections { get; } = new List<MongoDbCollectionOptions>();
    }

    public class MongoDbCollectionOptions
    {
        public string CollectionName { get; }
        public Type DocumentType { get; }
        public Type ContextType { get; set; }

        public MongoDbCollectionOptions(string collectionName, Type documentType)
        {
            CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            DocumentType = documentType ?? throw new ArgumentNullException(nameof(documentType));
        }

        internal IMongoDbCollectionContext BuildContext()
        {
            var contextType = ContextType;
            if (contextType == null)
                contextType = typeof(MongoDbCollectionContext<>).MakeGenericType(DocumentType);

            return (IMongoDbCollectionContext)Activator.CreateInstance(contextType);
        }
    }
}