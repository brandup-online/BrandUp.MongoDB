using System;
using Mongo2Go;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class Mongo2GoDbClientFactory : IMongoDbClientFactory, IDisposable
    {
        readonly MongoDbRunner runner;
        readonly MongoClient client;

        public MongoDbRunner Runner => runner;
        public MongoClient Client => client;

        public Mongo2GoDbClientFactory()
        {
            runner = MongoDbRunner.Start(singleNodeReplSet: true);
            client = new MongoClient(runner.ConnectionString);
        }

        public IMongoClient ResolveClient(string connectionString)
        {
            return client;
        }

        public void Dispose()
        {
            runner?.Dispose();
        }
    }
}