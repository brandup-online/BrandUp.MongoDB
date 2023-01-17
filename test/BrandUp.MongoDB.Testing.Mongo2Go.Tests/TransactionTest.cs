using BrandUp.MongoDB.Testing.Mongo2Go.Tests.Models;
using BrandUp.MongoDB.Transactions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class TransactionTest : IAsyncLifetime
    {
        private ServiceProvider rootServiceProvider;

        #region IAsyncLifetime members

        Task IAsyncLifetime.InitializeAsync()
        {
            var services = new ServiceCollection();
            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .AddMongo2GoDbClientFactory();

            rootServiceProvider = services.BuildServiceProvider();

            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await rootServiceProvider.DisposeAsync();
        }

        #endregion

        [Fact]
        public async Task Commit_Success()
        {
            #region Prepare

            using var scope = rootServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<TestDbContext>();

            #endregion

            var transactionFactory = scope.ServiceProvider.GetService<MongoDbTransactionFactory>();
            using var transaction = await transactionFactory.BeginAsync();

            await dbContext.Documents.InsertOneAsync(transactionFactory.Current, new Document { Name = "test" });

            var docs = dbContext.Documents.AsQueryable().Where(it => it.Name == "test");
            Assert.Single(docs);

            await transaction.CommitAsync();

            // Проверяем, что после коммита, документ созданный в транзакции есть.
            docs = dbContext.Documents.AsQueryable().Where(it => it.Name == "test");
            Assert.Single(docs);
        }
    }
}
