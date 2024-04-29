using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoDbClientFactory : IMongoDbClientFactory
    {
        public IMongoClient ResolveClient()
        {
            return new FakeMongoClient(MongoDbDefaults.LocalConnectionString);
        }
    }
}