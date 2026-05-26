using BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests
{
    public class IntegrationTest
    {
        [Fact]
        public async Task EmptyDatabase_EstimatedCount_IsZero()
        {
            var services = new ServiceCollection();

            services.AddEphemeralMongoDb();

            services.AddMongoDbContext<TestDbContext>(builder =>
            {
                builder.DatabaseName = "Test";
            });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetRequiredService<TestDbContext>();

            var count = await dbContext.Documents.EstimatedDocumentCountAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Insert_AndQuery_PersistsDocument()
        {
            var services = new ServiceCollection();

            services.AddEphemeralMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .UseCamelCaseElementName();

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetRequiredService<TestDbContext>();

            await dbContext.Documents.InsertOneAsync(
                new ArticleDocument { Name = "name", Author = "author" },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Collection(dbContext.Documents.AsQueryable(),
                d =>
                {
                    Assert.IsType<ArticleDocument>(d);
                    Assert.Equal("name", d.Name);
                    Assert.Equal("author", ((ArticleDocument)d).Author);
                });
        }
    }
}
