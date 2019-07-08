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
            services
                .AddSingleton<TestService>()
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .AddFakeMongoDbClientFactory();

            using (var scope = services.BuildServiceProvider())
            {
                var dbContext = scope.GetService<TestDbContext>();

                Assert.NotNull(dbContext);

                dbContext.Dispose();
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
                    { "IgnoreIfNull", "true" }
                }
            });

            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection();
            services
                .AddSingleton<TestService>()
                .AddMongoDbContext<TestDbContext>(configuration)
                .AddFakeMongoDbClientFactory();

            using (var scope = services.BuildServiceProvider())
            {
                var dbContext = scope.GetService<TestDbContext>();

                var pack = ConventionRegistry.Lookup(typeof(Document));
                Assert.NotEmpty(pack.Conventions.OfType<CamelCaseElementNameConvention>());
                Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
                Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());

                Assert.Equal("test", dbContext.Database.DatabaseNamespace.DatabaseName);

                dbContext.Dispose();
            }
        }

        [Fact]
        public void AddMongoDbContextExension()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestService>();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .AddFakeMongoDbClientFactory()
                .AddMongoDbContextExension<TestDbContext, IWorkerDbContext>();

            using (var scope = services.BuildServiceProvider())
            {
                var dbContext = scope.GetService<IWorkerDbContext>();

                Assert.NotNull(dbContext);
            }
        }

        [Fact]
        public void AddMongoDbContext_Diblicate()
        {
            var services1 = new ServiceCollection().AddSingleton<TestService>().AddMongoDbContext<TestDbContext>(builder => { builder.DatabaseName = "Test"; builder.UseCamelCaseElementName().UseIgnoreIfNull(true); }).AddFakeMongoDbClientFactory();
            var services2 = new ServiceCollection().AddSingleton<TestService>().AddMongoDbContext<TestDbContext>(builder => { builder.DatabaseName = "Test"; builder.UseCamelCaseElementName().UseIgnoreIfDefault(true); }).AddFakeMongoDbClientFactory();

            using (var scope1 = services1.BuildServiceProvider())
            {
                using (var scope2 = services2.BuildServiceProvider())
                {
                    var dbContext1 = scope1.GetService<TestDbContext>();
                    var dbContext2 = scope1.GetService<TestDbContext>();

                    Assert.NotNull(dbContext1);
                    Assert.NotNull(dbContext2);
                }
            }
        }

        #endregion
    }
}