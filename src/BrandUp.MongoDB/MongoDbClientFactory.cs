using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>Resolves the shared <see cref="IMongoClient"/> instance used by every <see cref="MongoDbContext"/>.</summary>
    public interface IMongoDbClientFactory
    {
        /// <summary>Returns the configured client. Implementations typically cache the instance.</summary>
        IMongoClient ResolveClient();
    }

    /// <summary>Global MongoDB connection configuration.</summary>
    public class MongoDbOptions
    {
        /// <summary>Connection string for the MongoDB server. Defaults to <see cref="MongoDbDefaults.LocalConnectionString"/>.</summary>
        public string ConnectionString { get; set; } = MongoDbDefaults.LocalConnectionString;
    }

    /// <summary>
    /// Default <see cref="IMongoDbClientFactory"/> implementation that caches one
    /// <see cref="IMongoClient"/> per connection-string-without-database key.
    /// </summary>
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