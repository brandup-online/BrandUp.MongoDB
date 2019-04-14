using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoDbClientFactory : IMongoDbClientFactory
    {
        public IMongoClient CreateClient(MongoUrl url)
        {
            return new FakeMongoClient(url);
        }
    }
}