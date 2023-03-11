using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class FixtureTest : IClassFixture<FakeMongoDbInstance>, IAsyncLifetime
    {
        readonly FakeMongoDbInstance fakeMongoDbInstance;

        public FixtureTest(FakeMongoDbInstance fakeMongoDbInstance)
        {
            this.fakeMongoDbInstance = fakeMongoDbInstance ?? throw new ArgumentNullException(nameof(fakeMongoDbInstance));
        }

        #region Test methods

        [Fact]
        public async void Test1()
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
        public async void Test2()
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
        readonly string[] SystemDatabaseNames = new string[] { "admin", "config", "local" };

        MongoDbRunner runner;
        MongoClient client;

        public MongoClient Client => client;

        #region IAsyncLifetime members

        Task IAsyncLifetime.InitializeAsync()
        {
            runner = MongoDbRunner.Start(singleNodeReplSet: true);
            client = new MongoClient(runner.ConnectionString);

            return Task.CompletedTask;
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
                if (SystemDatabaseNames.Contains(dbName, StringComparer.InvariantCultureIgnoreCase))
                    continue;

                await client.DropDatabaseAsync(dbName, cancellation.Token);
            }
        }
    }
}