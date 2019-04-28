using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using System.Collections.Generic;
using System.Linq;
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

            services.AddMongoDbContext<TestDbContext>(options =>
            {
                options.DatabaseName = "Test";
                options.UseFakeClientFactory();
            });

            using (var scope = services.BuildServiceProvider())
            {
                var dbContext = scope.GetService<TestDbContext>();

                Assert.NotNull(dbContext);
            }
        }

        [Fact]
        public void AddMongoDbContext_WithConfiguration()
        {
            var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            configurationBuilder.Add(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string> {
                    { "ConnectionString", MongoDbDefaults.LocalConnectionString },
                    { "DatabaseName", "test" },
                    { "CamelCase", "true" },
                    { "IgnoreIfNull", "true" },
                    { "IgnoreIfDefault", "true" }
                }
            });

            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection();
            services.AddMongoDbContext<TestDbContext>(configuration, options =>
            {
                options.UseFakeClientFactory();
            });

            using (var scope = services.BuildServiceProvider())
            {
                var dbContext = scope.GetService<TestDbContext>();

                var pack = ConventionRegistry.Lookup(typeof(Document));
                Assert.NotEmpty(pack.Conventions.OfType<CamelCaseElementNameConvention>());
                Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
                Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());

                Assert.Equal("test", dbContext.Database.DatabaseNamespace.DatabaseName);
            }
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

            using (var scope = services.BuildServiceProvider())
            {

                var dbContext = scope.GetService<IWorkerDbContext>();

                Assert.NotNull(dbContext);
            }
        }

        #endregion
    }
}