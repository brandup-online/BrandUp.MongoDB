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
                (collection) => { Assert.Equal("Documents", collection.Name); Assert.Equal(typeof(Document), collection.DocumentType); },
                (collection) => { Assert.Equal("Article", collection.Name); Assert.Equal(typeof(ArticleDocument), collection.DocumentType); },
                (collection) => { Assert.Equal("Tasks", collection.Name); Assert.Equal(typeof(TaskDocument), collection.DocumentType); });
        }

        [Fact]
        public void MultiParameterConstructor_AllParametersInjected()
        {
            // Regression: MongoDbContextBuilder.Build had a duplicate i++ that
            // skipped every second constructor argument when resolving services.
            var services = new ServiceCollection();

            services
                .AddSingleton<TestServiceA>()
                .AddSingleton<TestServiceB>()
                .AddSingleton<TestServiceC>()
                .AddFakeMongoDb();

            services.AddMongoDbContext<MultiCtorDbContext>(options =>
            {
                options.DatabaseName = "Test";
            });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetRequiredService<MultiCtorDbContext>();

            Assert.NotNull(dbContext.A);
            Assert.NotNull(dbContext.B);
            Assert.NotNull(dbContext.C);
            Assert.Same(scope.GetRequiredService<TestServiceA>(), dbContext.A);
            Assert.Same(scope.GetRequiredService<TestServiceB>(), dbContext.B);
            Assert.Same(scope.GetRequiredService<TestServiceC>(), dbContext.C);
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