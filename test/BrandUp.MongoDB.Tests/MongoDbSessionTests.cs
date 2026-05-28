using System;
using System.Threading.Tasks;
using BrandUp.MongoDB.Testing;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class MongoDbSessionTests
    {
        static MongoDbSession CreateSession()
        {
            return new MongoDbSession(new FakeMongoDbClientFactory());
        }

        [Fact]
        public async Task BeginAsync_StartsTransaction()
        {
            using var session = CreateSession();

            await using var transaction = await session.BeginAsync(TestContext.Current.CancellationToken);

            Assert.True(session.Current.IsInTransaction);
        }

        [Fact]
        public async Task CommitAsync_EndsTransaction()
        {
            using var session = CreateSession();

            var transaction = await session.BeginAsync(TestContext.Current.CancellationToken);
            await transaction.CommitAsync(TestContext.Current.CancellationToken);

            Assert.False(session.Current.IsInTransaction);
        }

        [Fact]
        public async Task Dispose_WithoutCommit_AbortsTransaction()
        {
            using var session = CreateSession();

            var transaction = await session.BeginAsync(TestContext.Current.CancellationToken);
            Assert.True(session.Current.IsInTransaction);

            transaction.Dispose();

            Assert.False(session.Current.IsInTransaction);
        }

        [Fact]
        public async Task DisposeAsync_WithoutCommit_AbortsTransaction()
        {
            using var session = CreateSession();

            var transaction = await session.BeginAsync(TestContext.Current.CancellationToken);
            await transaction.DisposeAsync();

            Assert.False(session.Current.IsInTransaction);
        }

        [Fact]
        public async Task BeginAsync_Nested_ChildCommitIsNoOp()
        {
            using var session = CreateSession();

            var outer = await session.BeginAsync(TestContext.Current.CancellationToken);
            var inner = await session.BeginAsync(TestContext.Current.CancellationToken);

            await inner.CommitAsync(TestContext.Current.CancellationToken);
            Assert.True(session.Current.IsInTransaction);

            await outer.CommitAsync(TestContext.Current.CancellationToken);
            Assert.False(session.Current.IsInTransaction);
        }

        [Fact]
        public async Task BeginAsync_SessionAlreadyInTransaction_AdoptsItWithoutNullReference()
        {
            using var session = CreateSession();

            // Start a transaction directly on the underlying handle, bypassing the session's own tracking.
            session.Current.StartTransaction();

            // BeginAsync must not dereference a null owner here; it adopts the active transaction instead.
            await using var transaction = await session.BeginAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(transaction);
            Assert.True(session.Current.IsInTransaction);
        }
    }
}
