using System.Threading.Tasks;
using BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests
{
    public class TransactionTest : IAsyncLifetime
    {
        ServiceProvider serviceProvider = null!;

        #region IAsyncLifetime members

        async ValueTask IAsyncLifetime.InitializeAsync()
        {
            var services = new ServiceCollection();

            services.AddEphemeralMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder => builder.DatabaseName = "Test")
                .UseCamelCaseElementName();

            serviceProvider = services.BuildServiceProvider(true);

            await Task.CompletedTask;
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await serviceProvider.DisposeAsync();
        }

        #endregion

        [Fact]
        public async Task Commit()
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var dbSession = scope.ServiceProvider.GetRequiredService<MongoDbSession>();

            await using (var transaction = await dbSession.BeginAsync(TestContext.Current.CancellationToken))
            {
                await dbContext.Documents.InsertOneAsync(dbSession.Current, new ArticleDocument { Name = "Test", Author = "test" }, cancellationToken: TestContext.Current.CancellationToken);

                await transaction.CommitAsync(TestContext.Current.CancellationToken);
            }

            var count = await dbContext.Documents.CountDocumentsAsync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Rollback()
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var dbSession = scope.ServiceProvider.GetRequiredService<MongoDbSession>();

            await using (var transaction = await dbSession.BeginAsync(TestContext.Current.CancellationToken))
            {
                await dbContext.Documents.InsertOneAsync(dbSession.Current, new ArticleDocument { Name = "Test", Author = "test" }, cancellationToken: TestContext.Current.CancellationToken);
                // No commit -> DisposeAsync aborts the transaction.
            }

            var count = await dbContext.Documents.CountDocumentsAsync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(0, count);
        }
    }
}
