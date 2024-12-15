using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing.Mongo2Go.Tests.Models
{
    public class TestDbContext : MongoDbContext
    {
        public IMongoCollection<Document> Documents => GetCollection<Document>();
    }

    [MongoCollection(CollectionName = "Documents")]
    [BsonDiscriminator("Base", Required = true, RootClass = true)]
    [BsonKnownTypes(typeof(ArticleDocument), typeof(NewsDocument))]
    public abstract class Document
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        [BsonRequired]
        public string Name { get; set; }
    }

    [BsonDiscriminator("Article", Required = true)]
    public class ArticleDocument : Document
    {
        [BsonRequired]
        public string Author { get; set; }
    }

    [BsonDiscriminator("News", Required = true)]
    public class NewsDocument : Document
    {
        [BsonRequired]
        public DateTime Date { get; set; }
    }
}