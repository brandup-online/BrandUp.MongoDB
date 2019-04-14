using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class DependencyInjectionTests : IDisposable
    {
        private readonly ServiceProvider scope;

        public DependencyInjectionTests()
        {
            var services = new ServiceCollection();

            services.AddMongoDbContext<TestDbContext>(options =>
            {
                options.DatabaseName = "Test";
                options.UseFakeClientFactory();
            });

            scope = services.BuildServiceProvider();
        }

        void IDisposable.Dispose()
        {
            scope.Dispose();
        }

        #region Test methods

        [Fact]
        public void AddMongoDbContext()
        {
            var services = new ServiceCollection();

            services.AddMongoDbContext<TestDbContext>(options =>
            {
                options.DatabaseName = "Test";
                options.UseFakeClientFactory();
            });

            var scope = services.BuildServiceProvider();

            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddMongoDbContextExension()
        {
            var services = new ServiceCollection();

            services.AddMongoDbContext<TestDbContext>(options =>
            {
                options.DatabaseName = "Test";
                options.UseFakeClientFactory();
            });
            services.AddMongoDbContextExension<TestDbContext, IWorkerDbContext>();

            var scope = services.BuildServiceProvider();

            var dbContext = scope.GetService<IWorkerDbContext>();

            Assert.NotNull(dbContext);
        }

        #endregion
    }
}