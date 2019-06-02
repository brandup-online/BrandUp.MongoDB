using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Linq;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class MongoDbContextBuilderTests : IDisposable
    {
        private readonly MongoDbContextBuilder<TestDbContext> builder;
        private readonly TestDbContext dbContext;

        public MongoDbContextBuilderTests()
        {
            builder = new MongoDbContextBuilder<TestDbContext>
            {
                DatabaseName = "Test"
            };

            builder.UseFakeClientFactory();

            var services = new ServiceCollection();
            services.AddSingleton<TestService>();
            var provider = services.BuildServiceProvider();

            dbContext = builder.Build(provider);
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }

        #region Test methods

        [Fact]
        public void CheckBuild()
        {
            var acticleCollectionContext = dbContext.GetCollectionContext<ArticleDocument>();
            var taskCollectionContext = dbContext.GetCollectionContext<TaskDocument>();

            Assert.IsAssignableFrom<MongoDbCollectionContext<ArticleDocument>>(acticleCollectionContext);
            Assert.IsAssignableFrom<TaskDocumentCollectionContext>(taskCollectionContext);

            Assert.True(((TaskDocumentCollectionContext)taskCollectionContext).IsGetCollectionSettings);
            Assert.True(((TaskDocumentCollectionContext)taskCollectionContext).IsGetCreationOptions);
            Assert.True(((TaskDocumentCollectionContext)taskCollectionContext).IsOnSetupCollection);
        }

        [Fact]
        public void CheckCollections()
        {
            Assert.Equal(2, builder.Collections.Count());
            Assert.True(builder.HasCollectionDocumentType(typeof(ArticleDocument)));
            Assert.True(builder.HasCollectionDocumentType(typeof(TaskDocument)));
            Assert.True(builder.HasDocumentType(typeof(SeoOptions)));
            Assert.True(builder.HasDocumentType(typeof(Tag)));
            Assert.True(builder.HasDocumentType(typeof(CommentDocument)));
            Assert.True(builder.HasCollectionName("Article"));
            Assert.True(builder.HasCollectionName("Tasks"));
        }

        [Fact]
        public void AddCollection()
        {
            builder.AddCollection(typeof(CommentDocument));

            Assert.True(builder.HasCollectionDocumentType(typeof(CommentDocument)));
        }

        [Fact]
        public void UseCamelCaseElementName()
        {
            builder.UseCamelCaseElementName();

            Assert.NotEmpty(builder.Conventions.OfType<CamelCaseElementNameConvention>());

            var pack = ConventionRegistry.Lookup(typeof(ArticleDocument));
            Assert.NotEmpty(pack.Conventions.OfType<CamelCaseElementNameConvention>());
        }

        [Fact]
        public void UseIgnoreIfNull()
        {
            builder.UseIgnoreIfNull(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfNullConvention>());

            var pack = ConventionRegistry.Lookup(typeof(ArticleDocument));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfNullConvention>());
        }

        [Fact]
        public void UseIgnoreIfDefault()
        {
            builder.UseIgnoreIfDefault(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfDefaultConvention>());

            var pack = ConventionRegistry.Lookup(typeof(ArticleDocument));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
        }

        [Fact]
        public void CheckConventionOfBaseType()
        {
            builder.UseIgnoreIfDefault(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfDefaultConvention>());

            var pack = ConventionRegistry.Lookup(typeof(Document));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
        }

        [Fact]
        public void CheckConventionOfKnownType()
        {
            builder.UseIgnoreIfDefault(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfDefaultConvention>());

            var pack = ConventionRegistry.Lookup(typeof(NewsDocument));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
        }

        #endregion
    }
}