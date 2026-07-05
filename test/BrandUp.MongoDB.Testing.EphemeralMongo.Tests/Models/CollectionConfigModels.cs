using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Models
{
    public class CappedEventsDbContext : MongoDbContext
    {
        public IMongoCollection<CappedEventDocument> Events => GetCollection<CappedEventDocument>();
    }

    [MongoCollection(CollectionName = "capped_events", Capped = true, CappedMaxSize = 64 * 1024, CappedMaxDocuments = 1000)]
    public class CappedEventDocument
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string? Message { get; set; }
    }

    public class CompressedEventsDbContext : MongoDbContext
    {
        public IMongoCollection<CompressedEventDocument> Events => GetCollection<CompressedEventDocument>();
    }

    [MongoCollection(CollectionName = "compressed_events", BlockCompressor = MongoBlockCompressor.Zstd)]
    public class CompressedEventDocument
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string? Message { get; set; }
    }

    public class ValidatedPeopleDbContext : MongoDbContext
    {
        public IMongoCollection<ValidatedPersonDocument> People => GetCollection<ValidatedPersonDocument>();
    }

    [MongoCollection(CollectionName = "validated_people")]
    public class ValidatedPersonDocument : IMongoCollectionConfiguration
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string? Name { get; set; }

        public static void Configure(MongoCollectionConfigurationBuilder builder)
            => builder.Validation(RequireNameSchema, DocumentValidationLevel.Strict, DocumentValidationAction.Error);

        internal static readonly BsonDocument RequireNameSchema = new("$jsonSchema", new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "name" } },
            { "properties", new BsonDocument("name", new BsonDocument("bsonType", "string")) }
        });
    }

    public class ReconcilePeopleDbContext : MongoDbContext
    {
        public IMongoCollection<ReconcilePersonDocument> People => GetCollection<ReconcilePersonDocument>();
    }

    [MongoCollection(CollectionName = "reconcile_people")]
    public class ReconcilePersonDocument : IMongoCollectionConfiguration
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string? Name { get; set; }

        public static void Configure(MongoCollectionConfigurationBuilder builder)
            => builder.Validation(ValidatedPersonDocument.RequireNameSchema, DocumentValidationLevel.Strict, DocumentValidationAction.Error);
    }
}
