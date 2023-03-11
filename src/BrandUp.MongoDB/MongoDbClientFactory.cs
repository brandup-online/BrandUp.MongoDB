using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public interface IMongoDbClientFactory
    {
        IMongoClient CreateClient(MongoUrl mongoUrl);
    }

    public class MongoDbClientFactory : IMongoDbClientFactory
    {
        static readonly Dictionary<string, IMongoClient> _clients = new();

        public IMongoClient CreateClient(MongoUrl mongoUrl)
        {
            if (mongoUrl == null)
                throw new ArgumentNullException(nameof(mongoUrl));

            var mongoUrlStr = mongoUrl.ToString();
            if (!_clients.ContainsKey(mongoUrlStr))
                _clients.Add(mongoUrlStr, new MongoClient(mongoUrl));

            return _clients[mongoUrlStr];
        }
    }
}