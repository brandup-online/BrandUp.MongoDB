using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Test1Async()
        {
            var services = new ServiceCollection();

            services.AddTestMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .UseCamelCaseElementName()
                .UseIgnoreIfDefault(false)
                .UseIgnoreIfNull(true);

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            var countDocuments = await dbContext.Documents.EstimatedDocumentCountAsync();

            Assert.Equal(0, countDocuments);
        }

        [Fact]
        public async Task Test2Async()
        {
            var services = new ServiceCollection();

            services.AddTestMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            await dbContext.Documents.InsertOneAsync(new ArticleDocument { Name = "name", Author = "author" });

            Assert.Collection(dbContext.Documents.AsQueryable(),
                d =>
                {
                    Assert.IsType<ArticleDocument>(d);

                    Assert.Equal("name", d.Name);
                    Assert.Equal("author", ((ArticleDocument)d).Author);
                });
        }

        [Fact]
        public async Task CollectionOfType()
        {
            var services = new ServiceCollection();

            services.AddTestMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            await dbContext.Documents.InsertOneAsync(new ArticleDocument { Name = "name1", Author = "author" });
            await dbContext.Documents.InsertOneAsync(new NewsDocument { Name = "name2", Date = DateTime.UtcNow });

            Assert.Collection(dbContext.Documents.OfType<ArticleDocument>().AsQueryable(),
                d =>
                {
                    Assert.IsType<ArticleDocument>(d);

                    Assert.Equal("name1", d.Name);
                });
        }

        [Fact]
        public async Task Test3Async()
        {
            var services = new ServiceCollection();

            services.AddTestMongoDb();

            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                });

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            var countDocuments = await dbContext.Documents.EstimatedDocumentCountAsync();

            Assert.Equal(0, countDocuments);
        }
    }

    public class TestDbContext : MongoDbContext
    {
        public IMongoCollection<Document> Documents => GetCollection<Document>();
    }

    [MongoCollection(CollectionName = "Documents")]
    public abstract class Document
    {
        [BsonId(IdGenerator = typeof(ObjectIdGenerator)), BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        [BsonRequired]
        public string Name { get; set; }
    }

    public class ArticleDocument : Document
    {
        [BsonRequired]
        public string Author { get; set; }
    }

    public class NewsDocument : Document
    {
        [BsonRequired]
        public DateTime Date { get; set; }
    }
}