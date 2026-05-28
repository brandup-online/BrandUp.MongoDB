using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class MongoDbContextBuilderTests
    {
        static MongoDbContextBuilder<TestDbContext> CreateBuilder()
        {
            return new MongoDbContextBuilder<TestDbContext>(new ServiceCollection());
        }

        [Fact]
        public void Ctor_RegistersContextCollections()
        {
            var builder = CreateBuilder();

            Assert.True(builder.HasCollectionType(typeof(Document)));
            Assert.True(builder.HasCollectionType(typeof(ArticleDocument)));
            Assert.True(builder.HasCollectionType(typeof(TaskDocument)));
        }

        [Fact]
        public void HasCollectionName_IsCaseInsensitive()
        {
            var builder = CreateBuilder();

            Assert.True(builder.HasCollectionName("Documents"));
            Assert.True(builder.HasCollectionName("documents"));
            Assert.False(builder.HasCollectionName("missing"));
        }

        [Fact]
        public void DocumentGraph_IncludesKnownTypesAndNestedTypes()
        {
            var builder = CreateBuilder();

            Assert.True(builder.HasDocumentType(typeof(NewsDocument)));      // [BsonKnownTypes]
            Assert.True(builder.HasDocumentType(typeof(SeoOptions)));        // object property
            Assert.True(builder.HasDocumentType(typeof(Tag)));              // List<Tag>
            Assert.True(builder.HasDocumentType(typeof(CommentDocument)));  // array property
        }

        [Fact]
        public void RegisterCollection_Duplicate_Throws()
        {
            var builder = CreateBuilder();

            Assert.Throws<ArgumentException>(() => builder.RegisterCollection(typeof(TaskDocument)));
        }

        [Fact]
        public void RegisterCollection_WithoutAttribute_Throws()
        {
            var builder = CreateBuilder();

            Assert.Throws<ArgumentException>(() => builder.RegisterCollection(typeof(NoAttributeDocument)));
        }

        [Fact]
        public void RegisterCollection_NotClass_Throws()
        {
            var builder = CreateBuilder();

            Assert.Throws<ArgumentException>(() => builder.RegisterCollection(typeof(int)));
        }

        [Fact]
        public void AddCollection_RegistersNewType()
        {
            IMongoDbContextBuilder builder = CreateBuilder();

            builder.AddCollection<StandaloneDocument>();

            Assert.True(builder.HasCollectionType(typeof(StandaloneDocument)));
        }

        public class NoAttributeDocument
        {
            public string Id { get; set; } = null!;
        }

        [MongoCollection]
        public class StandaloneDocument
        {
        }
    }
}
