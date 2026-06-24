using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Tests
{
    public class CollectionConfigurationTests
    {
        static TestDbContextForConfig BuildContext()
        {
            var services = new ServiceCollection();
            services.AddFakeMongoDb();
            services.AddMongoDbContext<TestDbContextForConfig>(options => options.DatabaseName = "Test");

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<TestDbContextForConfig>();
        }

        [Fact]
        public void Attribute_DeclaresCappedAndChangeStream()
        {
            var dbContext = BuildContext();

            var configuration = dbContext.GetCollectionContext<CappedLogDocument>().Configuration;

            Assert.True(configuration.IsCapped);
            Assert.Equal(1024L * 1024L, configuration.MaxSize);
            Assert.Equal(5000L, configuration.MaxDocuments);
            Assert.True(configuration.ChangeStreamPreAndPostImagesEnabled);
        }

        [Fact]
        public void Interface_DeclaresValidation()
        {
            var dbContext = BuildContext();

            var configuration = dbContext.GetCollectionContext<ValidatedDocument>().Configuration;

            Assert.NotNull(configuration.Validator);
            Assert.Equal(DocumentValidationLevel.Moderate, configuration.ValidationLevel);
            Assert.Equal(DocumentValidationAction.Error, configuration.ValidationAction);
        }

        [Fact]
        public void AttributeAndInterface_Compose()
        {
            var dbContext = BuildContext();

            var configuration = dbContext.GetCollectionContext<CombinedDocument>().Configuration;

            Assert.True(configuration.IsCapped);
            Assert.Equal(2048L, configuration.MaxSize);
            Assert.NotNull(configuration.Validator);
        }

        [Fact]
        public void CappedWithoutSize_Throws()
        {
            var services = new ServiceCollection();
            services.AddFakeMongoDb();

            // The metadata is constructed while registering the context's collections, so the invalid
            // attribute is rejected eagerly at registration time.
            Assert.Throws<ArgumentException>(() =>
                services.AddMongoDbContext<InvalidCappedDbContext>(options => options.DatabaseName = "Test"));
        }
    }

    public class TestDbContextForConfig : MongoDbContext
    {
        public IMongoCollection<CappedLogDocument> Logs => GetCollection<CappedLogDocument>();
        public IMongoCollection<ValidatedDocument> Validated => GetCollection<ValidatedDocument>();
        public IMongoCollection<CombinedDocument> Combined => GetCollection<CombinedDocument>();
    }

    public class InvalidCappedDbContext : MongoDbContext
    {
        public IMongoCollection<InvalidCappedDocument> Items => GetCollection<InvalidCappedDocument>();
    }

    [MongoCollection(Capped = true, CappedMaxSize = 1024 * 1024, CappedMaxDocuments = 5000, ChangeStreamPreAndPostImages = true)]
    public class CappedLogDocument
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
    }

    [MongoCollection]
    public class ValidatedDocument : IMongoCollectionConfiguration
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        public static void Configure(MongoCollectionConfigurationBuilder builder)
        {
            builder.Validation(
                new BsonDocument("$jsonSchema", new BsonDocument
                {
                    { "bsonType", "object" },
                    { "required", new BsonArray { "name" } }
                }),
                DocumentValidationLevel.Moderate,
                DocumentValidationAction.Error);
        }
    }

    [MongoCollection(Capped = true, CappedMaxSize = 2048)]
    public class CombinedDocument : IMongoCollectionConfiguration
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        public static void Configure(MongoCollectionConfigurationBuilder builder)
            => builder.Validation(new BsonDocument("$jsonSchema", new BsonDocument("bsonType", "object")));
    }

    [MongoCollection(Capped = true)]
    public class InvalidCappedDocument
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
    }
}
