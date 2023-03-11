using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoDbClientFactory : IMongoDbClientFactory
    {
        public IMongoClient ResolveClient(string connectionString)
        {
            return new FakeMongoClient(connectionString);
        }
    }
}