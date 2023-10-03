using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrandUp.MongoDB.Testing.Mongo2Go.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class IndexesTest : IClassFixture<FakeMongoDbInstance>, IAsyncLifetime
    {
        readonly FakeMongoDbInstance fakeMongoDbInstance;
        readonly ServiceProvider serviceProvider;
        readonly TestDbContext dbContext;

        public IndexesTest(FakeMongoDbInstance fakeMongoDbInstance)
        {
            this.fakeMongoDbInstance = fakeMongoDbInstance ?? throw new ArgumentNullException(nameof(fakeMongoDbInstance));

            var services = new ServiceCollection();

            services.AddSingleton<IMongoDbClientFactory>(fakeMongoDbInstance);

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            serviceProvider = services.BuildServiceProvider();
            dbContext = serviceProvider.GetService<TestDbContext>();
        }

        #region IAsyncLifetime members

        Task IAsyncLifetime.InitializeAsync()
        {
            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await serviceProvider.DisposeAsync();

            await fakeMongoDbInstance.CleanUpAsync();
        }

        #endregion

        #region Test methods

        [Fact]
        public async Task ApplyIndexes()
        {
            var indexes = new List<CreateIndexModel<Document>>
            {
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name), new CreateIndexOptions{ Name = "Name", Unique = true, Background = true })
            };

            var result = await dbContext.Documents.Indexes.ApplyIndexes(indexes, true);
            Assert.Collection(result, name => Assert.Equal("Name", name));

            result = await dbContext.Documents.Indexes.ApplyIndexes(indexes, true);
            Assert.Collection(result, name => Assert.Equal("Name", name));

            result = await dbContext.Documents.Indexes.ApplyIndexes(indexes, false);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ListNames()
        {
            var indexes = new List<CreateIndexModel<Document>>
            {
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name), new CreateIndexOptions{ Name = "Name", Unique = true, Background = true })
            };

            var names = await dbContext.Documents.Indexes.ListNamesAsync();
            Assert.Collection(names,
                name => Assert.Equal("_id_", name));

            await dbContext.Documents.Indexes.ApplyIndexes(indexes, true);

            names = await dbContext.Documents.Indexes.ListNamesAsync();
            Assert.Collection(names,
                name => Assert.Equal("_id_", name),
                name => Assert.Equal("Name", name));
        }

        [Fact]
        public async Task HasIndex()
        {
            var indexes = new List<CreateIndexModel<Document>>
            {
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name), new CreateIndexOptions{ Name = "Name", Unique = true, Background = true })
            };

            var result = await dbContext.Documents.Indexes.HasIndexAsync("Name");
            Assert.False(result);

            await dbContext.Documents.Indexes.ApplyIndexes(indexes, true);

            result = await dbContext.Documents.Indexes.HasIndexAsync("Name");
            Assert.True(result);

            result = await dbContext.Documents.Indexes.HasIndexAsync("name");
            Assert.True(result);
        }

        [Fact]
        public async Task DropIfExist()
        {
            var indexes = new List<CreateIndexModel<Document>>
            {
                new CreateIndexModel<Document>(Builders<Document>.IndexKeys.Ascending(it => it.Name), new CreateIndexOptions{ Name = "Name", Unique = true, Background = true })
            };

            var result = await dbContext.Documents.Indexes.DropIfExistAsync("Name");
            Assert.False(result);

            await dbContext.Documents.Indexes.ApplyIndexes(indexes, true);

            result = await dbContext.Documents.Indexes.DropIfExistAsync("Name");
            Assert.True(result);

            result = await dbContext.Documents.Indexes.DropIfExistAsync("name");
            Assert.False(result);
        }

        #endregion
    }
}