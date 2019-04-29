using MongoDB.Driver;
using System.Collections.Generic;

namespace BrandUp.MongoDB
{
    public interface IMongoDbClientFactory
    {
        IMongoClient CreateClient(MongoUrl mongoUrl);
    }

    public class MongoDbClientFactory : IMongoDbClientFactory
    {
        public static readonly MongoDbClientFactory Instance = new MongoDbClientFactory();
        private static readonly Dictionary<string, IMongoClient> _clients = new Dictionary<string, IMongoClient>();

        private MongoDbClientFactory() { }

        public IMongoClient CreateClient(MongoUrl mongoUrl)
        {
            var mongoUrlStr = mongoUrl.ToString();
            if (!_clients.ContainsKey(mongoUrlStr))
                _clients.Add(mongoUrlStr, new MongoClient(mongoUrl));

            return _clients[mongoUrlStr];
        }
    }
}