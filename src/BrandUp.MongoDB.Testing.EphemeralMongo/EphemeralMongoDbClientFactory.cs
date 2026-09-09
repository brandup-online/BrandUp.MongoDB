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

                // Defaults to one day, so every new mongod patch release triggers another
                // download and leaves one more copy behind in the binary cache.
                NewVersionCheckTimeout = TimeSpan.FromDays(365),
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
