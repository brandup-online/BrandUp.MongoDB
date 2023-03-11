using System;
using Mongo2Go;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class Mongo2GoDbClientFactory : IMongoDbClientFactory, IDisposable
    {
        readonly MongoDbRunner runner;
        MongoClient client;

        public Mongo2GoDbClientFactory()
        {
            runner = MongoDbRunner.Start(singleNodeReplSet: true);
            client = new MongoClient(runner.ConnectionString);
        }

        public IMongoClient CreateClient(MongoUrl url)
        {
            return client;
        }

        public void Dispose()
        {
            client = null;
            runner.Dispose();
        }
    }
}