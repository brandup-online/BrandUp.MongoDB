using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddMongoDbContext()
        {
            var services = new ServiceCollection();

            services.AddMongoDbContext<TestDbContext>(options =>
            {
                options.DatabaseName = "Test";
                options.ClientFactory = new FakeMongoDbClientFactory();
            });
            services.AddMongoDbContextExension<TestDbContext, IWorkerDbContext>();

            var scope = services.BuildServiceProvider();

            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddMongoDbContextImplementation()
        {
            var services = new ServiceCollection();

            services.AddMongoDbContext<TestDbContext>(options =>
            {
                options.DatabaseName = "Test";
                options.ClientFactory = new FakeMongoDbClientFactory();
            });
            services.AddMongoDbContextExension<TestDbContext, IWorkerDbContext>();

            var scope = services.BuildServiceProvider();

            var dbContext = scope.GetService<IWorkerDbContext>();

            Assert.NotNull(dbContext);
        }
    }
}