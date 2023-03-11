using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class DependencyInjectionTests
    {
        #region Test methods

        [Fact]
        public void AddMongoDbContext_WithOptionsAction()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<TestService>()
                .AddFakeMongoDbClientFactory();

            services
                .AddMongoDbContext<TestDbContext>(options =>
                {
                    options.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider(true);
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddMongoDbContextExension()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<TestService>()
                .AddFakeMongoDbClientFactory();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<IWorkerDbContext>();

            Assert.NotNull(dbContext);
        }

        #endregion
    }
}