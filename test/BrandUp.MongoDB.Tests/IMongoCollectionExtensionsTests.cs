using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandUp.MongoDB.Testing;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class IMongoCollectionExtensionsTests
    {
        readonly IMongoCollection<IndexDoc> collection;

        public IMongoCollectionExtensionsTests()
        {
            var client = new FakeMongoClient("mongodb://localhost:27017");
            collection = client.GetDatabase("test").GetCollection<IndexDoc>("docs");
        }

        static IEnumerable<CreateIndexModel<IndexDoc>> Indexes()
        {
            return
            [
                new CreateIndexModel<IndexDoc>(Builders<IndexDoc>.IndexKeys.Ascending(it => it.Name), new CreateIndexOptions { Name = "Name" })
            ];
        }

        [Fact]
        public async Task ApplyIndexes_CreatesIndex()
        {
            var names = await collection.Indexes.ApplyIndexes(Indexes(), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(["Name"], names.ToArray());
        }

        [Fact]
        public async Task ApplyIndexes_Recreate_ReturnsRecreatedName()
        {
            await collection.Indexes.ApplyIndexes(Indexes(), cancellationToken: TestContext.Current.CancellationToken);

            var names = await collection.Indexes.ApplyIndexes(Indexes(), recreateIfExists: true, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(["Name"], names.ToArray());
        }

        [Fact]
        public async Task ApplyIndexes_NoRecreate_SkipsExisting()
        {
            await collection.Indexes.ApplyIndexes(Indexes(), cancellationToken: TestContext.Current.CancellationToken);

            var names = await collection.Indexes.ApplyIndexes(Indexes(), recreateIfExists: false, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Empty(names);
        }

        [Fact]
        public async Task HasIndexAsync_IsCaseInsensitive()
        {
            await collection.Indexes.ApplyIndexes(Indexes(), cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(await collection.Indexes.HasIndexAsync("Name", TestContext.Current.CancellationToken));
            Assert.True(await collection.Indexes.HasIndexAsync("name", TestContext.Current.CancellationToken));
            Assert.False(await collection.Indexes.HasIndexAsync("other", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DropIfExistAsync_DropsOnlyWhenPresent()
        {
            await collection.Indexes.ApplyIndexes(Indexes(), cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(await collection.Indexes.DropIfExistAsync("Name", cancellationToken: TestContext.Current.CancellationToken));
            Assert.False(await collection.Indexes.DropIfExistAsync("Name", cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ListNamesAsync_ReturnsCreatedIndexes()
        {
            await collection.Indexes.ApplyIndexes(Indexes(), cancellationToken: TestContext.Current.CancellationToken);

            var names = await collection.Indexes.ListNamesAsync(TestContext.Current.CancellationToken);

            Assert.Contains("Name", names);
        }

        public class IndexDoc
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
        }
    }
}
