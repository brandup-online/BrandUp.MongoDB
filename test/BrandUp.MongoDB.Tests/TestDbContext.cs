﻿using MongoDB.Driver;
using System.Threading;

namespace BrandUp.MongoDB.Tests
{
    public class TestDbContext : MongoDbContext, IWorkerDbContext
    {
        public TestDbContext(MongoDbContextOptions options) : base(options) { }

        public IMongoCollection<ArticleDocument> Articles => GetCollection<ArticleDocument>();
        public IMongoCollection<TaskDocument> Tasks => GetCollection<TaskDocument>();
    }

    public interface IWorkerDbContext
    {
        IMongoCollection<TaskDocument> Tasks { get; }
    }

    [MongoDbDocument]
    public class ArticleDocument
    {

    }

    [MongoDbDocument(CollectionName = "Tasks", CollectionContextType = typeof(TaskDocumentCollectionContext))]
    public class TaskDocument
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

        protected override void OnSetupCollection(CancellationToken cancellationToken = default(CancellationToken))
        {
            IsOnSetupCollection = true;

            base.OnSetupCollection(cancellationToken);
        }
    }

    [MongoDbDocument]
    public class CommentDocument
    {

    }
}