using System.Threading.Tasks;
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
            var session = client.StartSession();

            Assert.NotNull(session);
            Assert.Equal(client, session.Client);
        }

        [Fact]
        public async Task StartSessionAsync()
        {
            var session = await client.StartSessionAsync();

            Assert.NotNull(session);
            Assert.Equal(client, session.Client);
        }
    }
}