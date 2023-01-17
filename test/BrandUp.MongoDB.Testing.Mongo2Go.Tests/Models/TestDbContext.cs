using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests.Models
{
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