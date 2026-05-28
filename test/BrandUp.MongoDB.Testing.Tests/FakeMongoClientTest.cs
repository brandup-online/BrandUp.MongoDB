using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoClientTest
    {
        readonly FakeMongoClient client;

        public FakeMongoClientTest()
        {
            client = new FakeMongoClient("mongodb://localhost:27017");
        }

        [Fact]
        public void GetDatabase()
        {
            var db1 = client.GetDatabase("test");
            var db2 = client.GetDatabase("test");

            Assert.NotNull(db1);
            Assert.Equal("test", db1.DatabaseNamespace.DatabaseName);
            Assert.Equal(db1, db2);
        }

        [Fact]
        public void StartSession()
        {
            var session = client.StartSession(cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(session);
            Assert.Equal(client, session.Client);
        }

        [Fact]
        public async Task StartSessionAsync()
        {
            var session = await client.StartSessionAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(session);
            Assert.Equal(client, session.Client);
        }

        [Fact]
        public void Cluster_NotNull()
        {
            Assert.NotNull(client.Cluster);
        }

        [Fact]
        public void With_ReturnsSameInstance()
        {
            Assert.Same(client, client.WithReadConcern(ReadConcern.Majority));
            Assert.Same(client, client.WithReadPreference(ReadPreference.Primary));
            Assert.Same(client, client.WithWriteConcern(WriteConcern.WMajority));
        }

        [Fact]
        public async Task ListDatabaseNames_ReflectsCreatedDatabases()
        {
            client.GetDatabase("alpha");
            client.GetDatabase("beta");

            var names = await (await client.ListDatabaseNamesAsync(TestContext.Current.CancellationToken))
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["alpha", "beta"], names.OrderBy(n => n).ToArray());
        }

        [Fact]
        public async Task ListDatabases_ReturnsDescriptionDocuments()
        {
            client.GetDatabase("alpha");

            var docs = await (await client.ListDatabasesAsync(TestContext.Current.CancellationToken))
                .ToListAsync(TestContext.Current.CancellationToken);

            var doc = Assert.Single(docs);
            Assert.Equal("alpha", doc["name"].AsString);
        }

        [Fact]
        public async Task DropDatabase_RemovesFromListing()
        {
            client.GetDatabase("alpha");
            client.GetDatabase("beta");

            await client.DropDatabaseAsync("alpha", TestContext.Current.CancellationToken);

            var names = await (await client.ListDatabaseNamesAsync(TestContext.Current.CancellationToken))
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["beta"], names.ToArray());
        }
    }
}