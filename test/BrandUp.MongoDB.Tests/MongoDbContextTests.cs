using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class MongoDbContextTests
    {
        static TestDbContext CreateContext()
        {
            var services = new ServiceCollection();
            services
                .AddSingleton<TestService>()
                .AddFakeMongoDb();
            services.AddMongoDbContext<TestDbContext>(options => options.DatabaseName = "Test");

            var scope = services.BuildServiceProvider();
            return scope.GetRequiredService<TestDbContext>();
        }

        [Fact]
        public void GetCollection_Registered_ReturnsCollection()
        {
            var dbContext = CreateContext();

            Assert.NotNull(dbContext.GetCollection<Document>());
            Assert.NotNull(dbContext.GetCollection<TaskDocument>());
        }

        [Fact]
        public void GetCollection_Unregistered_Throws()
        {
            var dbContext = CreateContext();

            // CommentDocument is reachable as a nested type but is not a registered collection.
            Assert.Throws<ArgumentException>(() => dbContext.GetCollection<CommentDocument>());
        }

        [Fact]
        public void TryGetCollectionContext_Registered_ReturnsTrue()
        {
            var dbContext = CreateContext();

            Assert.True(dbContext.TryGetCollectionContext<Document>(out var context));
            Assert.NotNull(context);
            Assert.Equal(typeof(Document), context.DocumentType);
        }

        [Fact]
        public void TryGetCollectionContext_Unregistered_ReturnsFalse()
        {
            var dbContext = CreateContext();

            Assert.False(dbContext.TryGetCollectionContext<CommentDocument>(out var context));
            Assert.Null(context);
        }

        [Fact]
        public void Collections_ExposesRegisteredMetadata()
        {
            var dbContext = CreateContext();

            Assert.Collection(dbContext.Collections,
                c => { Assert.Equal("Documents", c.Name); Assert.Equal(typeof(Document), c.DocumentType); },
                c => { Assert.Equal("Article", c.Name); Assert.Equal(typeof(ArticleDocument), c.DocumentType); },
                c => { Assert.Equal("Tasks", c.Name); Assert.Equal(typeof(TaskDocument), c.DocumentType); });
        }
    }

    public class MongoDbCollectionMetadataTests
    {
        [Theory]
        [InlineData("ArticleDocument", "Article")]
        [InlineData("UserModel", "User")]
        [InlineData("Plain", "Plain")]
        public void TrimCollectionNamePrefix_StripsKnownSuffixes(string input, string expected)
        {
            Assert.Equal(expected, MongoDbCollectionMetadata<TaskDocument>.TrimCollectionNamePrefix(input));
        }
    }

    public class OptionsValidationTests
    {
        [Fact]
        public void Validator_MissingDatabaseName_Fails()
        {
            var validator = new MongoDbContextOptionsValidator();

            var result = validator.Validate(null, new MongoDbContextOptions());

            Assert.True(result.Failed);
        }

        [Fact]
        public void Validator_WithDatabaseName_Succeeds()
        {
            var validator = new MongoDbContextOptionsValidator();

            var result = validator.Validate(null, new MongoDbContextOptions { DatabaseName = "x" });

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void AddMongoDb_EmptyConnectionString_FailsValidation()
        {
            var services = new ServiceCollection();
            services.AddMongoDb(options => options.ConnectionString = "");

            using var serviceProvider = services.BuildServiceProvider();

            Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(
                () => serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value);
        }
    }
}
