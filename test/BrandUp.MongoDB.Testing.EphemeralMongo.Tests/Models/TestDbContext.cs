using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Models
{
    public class TestDbContext : MongoDbContext
    {
        public IMongoCollection<Document> Documents => GetCollection<Document>();
    }

    [MongoCollection(CollectionName = "Documents")]
    [BsonDiscriminator("Base", RootClass = true)]
    [BsonKnownTypes(typeof(ArticleDocument))]
    public abstract class Document
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        [BsonRequired]
        public string Name { get; set; } = null!;
    }

    [BsonDiscriminator("Article", Required = true)]
    public class ArticleDocument : Document
    {
        [BsonRequired]
        public string Author { get; set; } = null!;
    }
}
