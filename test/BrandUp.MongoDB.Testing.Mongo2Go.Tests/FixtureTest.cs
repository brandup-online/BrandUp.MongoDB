using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandUp.MongoDB.Testing.Mongo2Go.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class FixtureTest(FakeMongoDbInstance fakeMongoDbInstance) : IClassFixture<FakeMongoDbInstance>, IAsyncLifetime
    {
        #region Test methods

        [Fact]
        public async Task Test1()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IMongoDbClientFactory>(fakeMongoDbInstance);

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            await dbContext.Documents.InsertOneAsync(new ArticleDocument { Name = "name", Author = "author" });

            Assert.Single(dbContext.Documents.AsQueryable());
        }

        [Fact]
        public async Task Test2()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IMongoDbClientFactory>(fakeMongoDbInstance);

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            await dbContext.Documents.InsertOneAsync(new ArticleDocument { Name = "name", Author = "author" });

            Assert.Single(dbContext.Documents.AsQueryable());
        }

        #endregion

        #region IAsyncLifetime members

        Task IAsyncLifetime.InitializeAsync()
        {
            return Task.CompletedTask;
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return fakeMongoDbInstance.CleanUpAsync();
        }

        #endregion
    }

    public class FakeMongoDbInstance : IMongoDbClientFactory, IAsyncLifetime
    {
        MongoDbRunner runner;
        MongoClient client;
        List<string> systemDatabaseNames;

        public MongoClient Client => client;

        #region IAsyncLifetime members

        async Task IAsyncLifetime.InitializeAsync()
        {
            runner = MongoDbRunner.Start(singleNodeReplSet: true);
            client = new MongoClient(runner.ConnectionString);

            systemDatabaseNames = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            runner?.Dispose();

            return Task.CompletedTask;
        }

        #endregion

        #region IMongoDbClientFactory members

        IMongoClient IMongoDbClientFactory.ResolveClient(string connectionString)
        {
            return client;
        }

        #endregion

        public async Task CleanUpAsync()
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var databaseNames = await (await client.ListDatabaseNamesAsync(new ListDatabaseNamesOptions { AuthorizedDatabases = true }, cancellation.Token)).ToListAsync(cancellation.Token);
            foreach (var dbName in databaseNames)
            {
                if (systemDatabaseNames.Contains(dbName, StringComparer.InvariantCultureIgnoreCase))
                    continue;

                await client.DropDatabaseAsync(dbName, cancellation.Token);
            }
        }
    }
}