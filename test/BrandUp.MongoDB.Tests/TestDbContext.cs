using MongoDB.Driver;
using System.Threading;

namespace BrandUp.MongoDB.Tests
{
    public class TestDbContext : MongoDbContext, IWorkerDbContext
    {
        private readonly TestService testService;

        public TestDbContext(MongoDbContextOptions options, TestService testService) : base(options)
        {
            this.testService = testService ?? throw new System.ArgumentNullException(nameof(testService));
        }

        public IMongoCollection<ArticleDocument> Articles => GetCollection<ArticleDocument>();
        public IMongoCollection<TaskDocument> Tasks => GetCollection<TaskDocument>();
    }

    public class TestService
    {

    }

    public interface IWorkerDbContext
    {
        IMongoCollection<TaskDocument> Tasks { get; }
    }

    public class Document
    {

    }

    [Document]
    [DocumentKnownType(typeof(NewsDocument))]
    public class ArticleDocument : Document
    {

    }

    public class NewsDocument : ArticleDocument
    {

    }

    [Document(CollectionName = "Tasks", CollectionContextType = typeof(TaskDocumentCollectionContext))]
    public class TaskDocument : Document
    {

    }

    public class TaskDocumentCollectionContext : MongoDbCollectionContext<TaskDocument>
    {
        public bool IsGetCollectionSettings { get; private set; }
        public bool IsGetCreationOptions { get; private set; }
        public bool IsOnSetupCollection { get; private set; }

        protected override MongoCollectionSettings GetCollectionSettings()
        {
            IsGetCollectionSettings = true;

            return base.GetCollectionSettings();
        }

        protected override CreateCollectionOptions GetCreationOptions()
        {
            IsGetCreationOptions = true;

            return base.GetCreationOptions();
        }

        protected override void OnSetupCollection(CancellationToken cancellationToken = default)
        {
            IsOnSetupCollection = true;

            base.OnSetupCollection(cancellationToken);
        }
    }

    [Document]
    public class CommentDocument : Document
    {

    }
}