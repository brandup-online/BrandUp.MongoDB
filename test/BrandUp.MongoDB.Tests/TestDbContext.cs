using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Tests
{
    public class TestDbContext : MongoDbContext, IWorkerDbContext
    {
        readonly TestService testService;

        public TestDbContext(TestService testService)
        {
            this.testService = testService ?? throw new System.ArgumentNullException(nameof(testService));
        }

        public IMongoCollection<Document> Documents => GetCollection<Document>();
        public IMongoCollection<ArticleDocument> Articles => GetCollection<ArticleDocument>();
        public IMongoCollection<TaskDocument> Tasks => GetCollection<TaskDocument>();
    }

    public class TestService { }

    public interface IWorkerDbContext
    {
        IMongoCollection<TaskDocument> Tasks { get; }
    }

    [MongoCollection(CollectionName = "Documents")]
    [BsonKnownTypes(typeof(ArticleDocument))]
    public abstract class Document
    {
        [BsonId(IdGenerator = typeof(ObjectIdGenerator)), BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }

    [MongoCollection]
    [BsonKnownTypes(typeof(NewsDocument), typeof(News2Document))]
    public class ArticleDocument : Document
    {
        public SeoOptions Seo { get; set; }
        public List<Tag> Tags { get; set; }
        public CommentDocument[] Comments { get; set; }
    }

    public class NewsDocument : ArticleDocument
    {

    }

    public class News2Document : ArticleDocument
    {

    }

    public class SeoOptions
    {

    }

    public class Tag
    {

    }

    [MongoCollection(CollectionName = "Tasks")]
    public class TaskDocument : Document
    {

    }

    [MongoCollection]
    public class CommentDocument : Document
    {

    }
}