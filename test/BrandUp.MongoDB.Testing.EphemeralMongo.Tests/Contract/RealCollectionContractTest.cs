using System;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Contract
{
    /// <summary>
    /// Runs the collection contract against a real mongod started by EphemeralMongo
    /// as a single-node replica set (required for the transaction scenarios).
    /// </summary>
    public class RealCollectionContractTest : CollectionContractTest, IClassFixture<RealCollectionContractTest.MongoFixture>
    {
        readonly MongoFixture fixture;

        public RealCollectionContractTest(MongoFixture fixture)
        {
            this.fixture = fixture;
        }

        protected override IMongoClient GetClientOrSkip()
        {
            return fixture.GetClientOrSkip();
        }

        public sealed class MongoFixture : IDisposable
        {
            // Lazy caches the startup exception, so after one failure every test skips instantly.
            readonly Lazy<EphemeralMongoDbClientFactory> factory = new(() => new EphemeralMongoDbClientFactory());

            public IMongoClient GetClientOrSkip()
            {
                try
                {
                    return factory.Value.Client;
                }
                catch (global::EphemeralMongo.EphemeralMongoException ex)
                {
                    Assert.Skip($"EphemeralMongo could not start mongod in this environment: {ex.Message}");
                    throw;
                }
            }

            public void Dispose()
            {
                if (factory.IsValueCreated)
                    factory.Value.Dispose();
            }
        }
    }
}
