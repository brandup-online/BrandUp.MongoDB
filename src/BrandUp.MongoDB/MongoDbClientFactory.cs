using System;
using System.Collections.Concurrent;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public interface IMongoDbClientFactory
    {
        IMongoClient ResolveClient(string connectionString);
    }

    public class MongoDbClientFactory : IMongoDbClientFactory
    {
        static readonly ConcurrentDictionary<string, IMongoClient> clients = new();

        public IMongoClient ResolveClient(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            var mongoUrlBuilder = new MongoUrlBuilder(connectionString)
            {
                DatabaseName = null
            };
            var mongoUrl = mongoUrlBuilder.ToMongoUrl();
            var mongoUrlStr = mongoUrl.Url.ToLower();

            var mongoClient = clients.GetOrAdd(mongoUrlStr, _ =>
            {
                return new MongoClient(mongoUrl);
            });

            return mongoClient;
        }
    }
}