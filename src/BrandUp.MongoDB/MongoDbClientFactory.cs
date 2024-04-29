using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public interface IMongoDbClientFactory
    {
        IMongoClient ResolveClient();
    }

    public class MongoDbOptions
    {
        public string ConnectionString { get; set; } = MongoDbDefaults.LocalConnectionString;
    }

    public class MongoDbClientFactory(IOptions<MongoDbOptions> options) : IMongoDbClientFactory
    {
        static readonly ConcurrentDictionary<string, IMongoClient> clients = [];

        public IMongoClient ResolveClient()
        {
            var connectionString = options.Value.ConnectionString;

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