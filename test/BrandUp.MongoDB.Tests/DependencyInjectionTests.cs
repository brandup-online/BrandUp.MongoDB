using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class DependencyInjectionTests
    {
        #region Test methods

        [Fact]
        public void CheckOptions_Success()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<TestService>()
                .AddFakeMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(options =>
                {
                    options.ConnectionString = MongoDbDefaults.LocalConnectionString;
                    options.DatabaseName = "Test";
                })
                .UseCamelCaseElementName()
                .UseIgnoreIfDefault(false)
                .UseIgnoreIfNull(true);

            using var scope = services.BuildServiceProvider(true);
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);
            Assert.Equal("Test", dbContext.Database.DatabaseNamespace.DatabaseName);

            Assert.Collection(dbContext.Collections,
                (collection) => { Assert.Equal("Documents", collection.CollectionName); Assert.Equal(typeof(Document), collection.DocumentType); },
                (collection) => { Assert.Equal("Article", collection.CollectionName); Assert.Equal(typeof(ArticleDocument), collection.DocumentType); },
                (collection) => { Assert.Equal("Tasks", collection.CollectionName); Assert.Equal(typeof(TaskDocument), collection.DocumentType); });
        }

        [Fact]
        public void ContextExension()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<TestService>()
                .AddFakeMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(options =>
                {
                    options.ConnectionString = MongoDbDefaults.LocalConnectionString;
                    options.DatabaseName = "Test";
                })
                .AddExtension<TestDbContext, IWorkerDbContext>();

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<IWorkerDbContext>();

            Assert.NotNull(dbContext);
        }

        #endregion
    }
}