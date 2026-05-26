using EphemeralMongo;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class EphemeralMongoDbClientFactory : IMongoDbClientFactory, IDisposable
    {
        readonly IMongoRunner runner;
        readonly MongoClient client;

        public IMongoRunner Runner => runner;
        public MongoClient Client => client;

        public EphemeralMongoDbClientFactory()
        {
            runner = MongoRunner.Run(new MongoRunnerOptions
            {
                UseSingleNodeReplicaSet = true,
            });
            client = new MongoClient(runner.ConnectionString);
        }

        public IMongoClient ResolveClient()
        {
            return client;
        }

        public void Dispose()
        {
            runner.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
