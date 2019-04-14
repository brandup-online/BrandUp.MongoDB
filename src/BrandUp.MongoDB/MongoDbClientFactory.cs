using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public interface IMongoDbClientFactory
    {
        IMongoClient CreateClient(MongoUrl url);
    }

    public class MongoDbClientFactory : IMongoDbClientFactory
    {
        public IMongoClient CreateClient(MongoUrl url)
        {
            return new MongoClient(url);
        }
    }
}