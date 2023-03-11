using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class MongoDbContextBuilderTests : IAsyncLifetime
    {
        MongoDbContextBuilder<TestDbContext> builder;
        TestDbContext dbContext;

        #region IAsyncLifetime members

        Task IAsyncLifetime.InitializeAsync()
        {
            builder = new MongoDbContextBuilder<TestDbContext>
            {
                DatabaseName = "Test"
            };

            var services = new ServiceCollection();
            services
                .AddSingleton<TestService>()
                .AddFakeMongoDbClientFactory();
            var provider = services.BuildServiceProvider();

            dbContext = builder.Build(provider);

            return Task.CompletedTask;
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            dbContext.Dispose();

            return Task.CompletedTask;
        }

        #endregion

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
            Assert.Equal(3, builder.Collections.Count());
            Assert.True(builder.HasCollectionDocumentType(typeof(ArticleDocument)));
            Assert.True(builder.HasCollectionDocumentType(typeof(TaskDocument)));
            Assert.True(builder.HasCollectionDocumentType(typeof(Document)));
            Assert.True(builder.HasDocumentType(typeof(SeoOptions)));
            Assert.True(builder.HasDocumentType(typeof(Tag)));
            Assert.True(builder.HasDocumentType(typeof(CommentDocument)));
            Assert.True(builder.HasDocumentType(typeof(NewsDocument)));
            Assert.True(builder.HasDocumentType(typeof(News2Document)));
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

        [Fact]
        public void CheckConventionOfPropertyObject()
        {
            builder.UseIgnoreIfDefault(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfDefaultConvention>());

            var pack = ConventionRegistry.Lookup(typeof(SeoOptions));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
        }

        [Fact]
        public void CheckConventionOfPropertyArray()
        {
            builder.UseIgnoreIfDefault(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfDefaultConvention>());

            var pack = ConventionRegistry.Lookup(typeof(CommentDocument));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
        }

        [Fact]
        public void CheckConventionOfPropertyEnumerable()
        {
            builder.UseIgnoreIfDefault(false);

            Assert.NotEmpty(builder.Conventions.OfType<IgnoreIfDefaultConvention>());

            var pack = ConventionRegistry.Lookup(typeof(Tag));
            Assert.NotEmpty(pack.Conventions.OfType<IgnoreIfDefaultConvention>());
        }

        [Fact]
        public void Insert()
        {
            dbContext.Documents.InsertOne(new ArticleDocument { Id = ObjectId.GenerateNewId().ToString() });

            //var list = dbContext.Documents.FindSync(Builders<Document>.Filter.Empty).ToList();
        }

        #endregion
    }
}