using Mongo2Go;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace BrandUp.MongoDB.Testing
{
    public class Mongo2GoDbClientFactory : MongoDbClientFactory, IDisposable
    {
        readonly Dictionary<string, MongoDbRunner> runners = new();

        protected override IMongoClient OnCreateClient(string key, MongoUrl mongoUrl)
        {
            var runner = MongoDbRunner.Start(singleNodeReplSet: true);
            runners.Add(key, runner);

            return base.OnCreateClient(key, MongoUrl.Create(runner.ConnectionString));
        }

        public void Dispose()
        {
            foreach (var runner in runners.Values)
                runner.Dispose();
        }
    }
}