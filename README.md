# BrandUp.MongoDB

## Installation

## Use

```
public class WebSiteDbContext : MongoDbContext, ICommentsDbContext
{
    public TestDbContext(MongoDbContextOptions options) : base(options) { }

    public IMongoCollection<ArticleDocument> Articles => GetCollection<ArticleDocument>();
    
    public IMongoCollection<CommentDocument> Comments => GetCollection<CommentDocument>();
}

public interface ICommentsDbContext
{
    IMongoCollection<CommentDocument> Comments { get; }
}

[MongoDbDocument(CollectionName = "Articles")]
public class ArticleDocument
{
}

[MongoDbDocument(CollectionName = "Comments")]
public class CommentDocument
{
}

services.AddMongoDbContext<WebSiteDbContext>(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "WebSite";
});
services.AddMongoDbContextExension<WebSiteDbContext, ICommentsDbContext>();
```
