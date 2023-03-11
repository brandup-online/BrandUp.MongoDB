using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeClientSessionHandleTest
    {
        readonly FakeMongoClient client;

        public FakeClientSessionHandleTest()
        {
            client = new FakeMongoClient("mongodb://localhost:27017");
        }

        [Fact]
        public void CommitTransaction()
        {
            using (var session = client.StartSession())
            {
                Assert.False(session.IsInTransaction);

                session.StartTransaction();

                Assert.True(session.IsInTransaction);

                session.CommitTransaction();

                Assert.False(session.IsInTransaction);
            }
        }

        [Fact]
        public void AbortTransaction()
        {
            using (var session = client.StartSession())
            {
                Assert.False(session.IsInTransaction);

                session.StartTransaction();

                Assert.True(session.IsInTransaction);

                session.AbortTransaction();

                Assert.False(session.IsInTransaction);
            }
        }
    }
}