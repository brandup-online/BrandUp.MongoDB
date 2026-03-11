using System;
using System.Threading.Tasks;
using BrandUp.MongoDB.Testing.Mongo2Go.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class TransactionTest : IAsyncLifetime
    {
        ServiceProvider serviceProvider;

        #region IAsyncLifetime members

        async ValueTask IAsyncLifetime.InitializeAsync()
        {
            var services = new ServiceCollection();

            services.AddTestMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .UseCamelCaseElementName()
                .UseIgnoreIfDefault(false)
                .UseIgnoreIfNull(true);

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
            await using var scope1 = serviceProvider.CreateAsyncScope();
            await using var scope2 = serviceProvider.CreateAsyncScope();

            var dbContext = scope1.ServiceProvider.GetService<TestDbContext>();
            var transactionFactory1 = scope1.ServiceProvider.GetService<ITransactionFactory>();
            var dbSession1 = scope1.ServiceProvider.GetService<MongoDbSession>();
            var transactionFactory2 = scope2.ServiceProvider.GetService<ITransactionFactory>();
            var dbSession2 = scope2.ServiceProvider.GetService<MongoDbSession>();

            using var transaction1 = await dbSession1.BeginAsync(TestContext.Current.CancellationToken);
            await dbContext.Documents.InsertOneAsync(dbSession1.Current, new ArticleDocument { Name = "Test", Author = "test" }, cancellationToken: TestContext.Current.CancellationToken);

            var countDocuments = await dbContext.Documents.EstimatedDocumentCountAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, countDocuments);

            // Проверяем, что добавленный документ не доступен в другой транзакции
            using var transaction2 = await transactionFactory2.BeginAsync(TestContext.Current.CancellationToken);
            countDocuments = await dbContext.Documents.CountDocumentsAsync(dbSession2.Current, Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(0, countDocuments);

            // Проверяем, что добавленный элемент не доступен без транзакции
            countDocuments = await dbContext.Documents.CountDocumentsAsync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(0, countDocuments);

            await transaction1.CommitAsync(TestContext.Current.CancellationToken);

            countDocuments = await dbContext.Documents.EstimatedDocumentCountAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, countDocuments);
        }

        [Fact]
        public async Task Rollback()
        {
            await using var scope1 = serviceProvider.CreateAsyncScope();

            var dbContext = scope1.ServiceProvider.GetService<TestDbContext>();
            var transactionFactory1 = scope1.ServiceProvider.GetService<ITransactionFactory>();
            var dbSession1 = scope1.ServiceProvider.GetService<MongoDbSession>();

            using var transaction1 = await transactionFactory1.BeginAsync(TestContext.Current.CancellationToken);
            await dbContext.Documents.InsertOneAsync(dbSession1.Current, new ArticleDocument { Name = "Test", Author = "test" }, cancellationToken: TestContext.Current.CancellationToken);
            transaction1.Dispose();

            var countDocuments = await dbContext.Documents.CountDocumentsAsync(Builders<Document>.Filter.Empty, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(0, countDocuments);
        }
    }
}