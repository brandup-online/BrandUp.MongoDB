using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Фабрика клиентов MongoDb.
    /// </summary>
    public interface IMongoDbClientFactory
    {
        IMongoClient GetClient(MongoUrl mongoUrl);
    }

    public class MongoDbClientFactory : IMongoDbClientFactory
    {
        readonly Dictionary<string, IMongoClient> clients = new();

        protected MongoDbClientFactory() { }

        #region IMongoDbClientFactory members

        public IMongoClient GetClient(MongoUrl mongoUrl)
        {
            if (mongoUrl == null)
                throw new ArgumentNullException(nameof(mongoUrl));

            var mongoUrlStr = mongoUrl.ToString();
            if (!clients.ContainsKey(mongoUrlStr))
            {
                var client = OnCreateClient(mongoUrlStr, mongoUrl);

                clients.Add(mongoUrlStr, client);
            }

            return clients[mongoUrlStr];
        }

        #endregion

        protected virtual IMongoClient OnCreateClient(string key, MongoUrl mongoUrl)
        {
            return new MongoClient(mongoUrl);
        }
    }
}