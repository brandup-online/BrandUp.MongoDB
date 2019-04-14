using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;

namespace BrandUp.MongoDB
{
    public class MongoDbCollectionContext<TDocument> : IMongoDbCollectionContext
        where TDocument : class
    {
        public IMongoCollection<TDocument> Collection { get; private set; }

        public MongoDbCollectionContext()
        {
            DocumentType = typeof(TDocument);
        }

        #region IMongoDbCollectionContext members

        public string CollectionName { get; private set; }
        public Type DocumentType { get; }
        void IMongoDbCollectionContext.Initialize(string collectionName, MongoDbContext dbContext, CancellationToken cancellationToken)
        {
            CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));

            bool needSetup = false;
            var collectionNames = dbContext.Database.ListCollectionNames(cancellationToken: cancellationToken).ToList(cancellationToken);
            if (!collectionNames.Any(it => it.ToLower() == CollectionName.ToLower()))
            {
                dbContext.Database.CreateCollection(CollectionName, GetCreationOptions(), cancellationToken);
                needSetup = true;
            }

            var collectionSettings = GetCollectionSettings();
            if (collectionSettings == null)
                collectionSettings = new MongoCollectionSettings();

            Collection = dbContext.Database.GetCollection<TDocument>(CollectionName, collectionSettings);

            if (needSetup)
                OnSetupCollection(cancellationToken);
        }

        #endregion

        protected virtual CreateCollectionOptions GetCreationOptions()
        {
            return null;
        }
        protected virtual MongoCollectionSettings GetCollectionSettings()
        {
            return null;
        }
        protected virtual void OnSetupCollection(CancellationToken cancellationToken = default)
        {
        }
    }

    internal interface IMongoDbCollectionContext
    {
        string CollectionName { get; }
        Type DocumentType { get; }
        void Initialize(string collectionName, MongoDbContext dbContext, CancellationToken cancellationToken = default);
    }
}