using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Test1Async()
        {
            var services = new ServiceCollection();
            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .AddMongo2GoDbClientFactory();

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            var countDocuments = await dbContext.Documents.EstimatedDocumentCountAsync();

            Assert.Equal(0, countDocuments);

            dbContext.Dispose();
        }

        [Fact]
        public async Task Test2Async()
        {
            var services = new ServiceCollection();
            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .AddMongo2GoDbClientFactory();

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            await dbContext.Documents.InsertOneAsync(new Document { Name = "test" });

            var docs = dbContext.Documents.AsQueryable().Where(it => it.Name == "test");
            Assert.Single(docs);

            dbContext.Dispose();
        }

        [Fact]
        public async Task Test3Async()
        {
            var services = new ServiceCollection();
            services
                .AddMongoDbContext<TestDbContext>(builder =>
                {
                    builder.DatabaseName = "Test";
                })
                .AddMongo2GoDbClientFactory();

            using var scope = services.BuildServiceProvider();
            var dbContext = scope.GetService<TestDbContext>();

            Assert.NotNull(dbContext);

            var countDocuments = await dbContext.Documents.EstimatedDocumentCountAsync();

            Assert.Equal(0, countDocuments);

            dbContext.Dispose();
        }
    }

    public class TestDbContext : MongoDbContext
    {
        public TestDbContext(MongoDbContextOptions options) : base(options) { }

        public IMongoCollection<Document> Documents => GetCollection<Document>();
    }

    [Document(CollectionName = "Documents")]
    public class Document
    {
        [BsonId(IdGenerator = typeof(ObjectIdGenerator)), BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
    }
}
