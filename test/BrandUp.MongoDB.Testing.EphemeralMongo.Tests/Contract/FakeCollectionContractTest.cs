using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Contract
{
    /// <summary>Runs the collection contract against the in-memory fake.</summary>
    public class FakeCollectionContractTest : CollectionContractTest
    {
        readonly FakeMongoClient client = new("mongodb://localhost:27017");

        protected override IMongoClient GetClientOrSkip()
        {
            return client;
        }
    }
}
