# BrandUp.MongoDB

[![Build Status](https://dev.azure.com/brandup/BrandUp%20Core/_apis/build/status/BrandUp.Worker?branchName=master)](https://dev.azure.com/brandup/BrandUp%20Core/_build/latest?definitionId=14&branchName=master)

## Installation
NuGet-package: [https://www.nuget.org/packages/BrandUp.MongoDB/](https://www.nuget.org/packages/BrandUp.MongoDB/)

## Configuration

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

[Document(CollectionName = "Articles")]
public class ArticleDocument { }

[Document(CollectionName = "Comments")]
public class CommentDocument { }

// Manual configuration
services.AddMongoDbContext<WebSiteDbContext>(builder =>
{
	builder.ConnectionString = "mongodb://localhost:27017";
	builder.DatabaseName = "WebSite";
	builder
		.UseCamelCaseElementName()
		.UseIgnoreIfNull()
		.UseIgnoreIfDefault();
});

// Configuration by appsettings.json
services.AddMongoDbContext<WebSiteDbContext>(configuration.GetSection("MongoDb:Website"));

// Register interface
services.AddMongoDbContextExension<WebSiteDbContext, ICommentsDbContext>();
```

## Using

```
var dbContext = serviceProvider.GetRequiredService<WebSiteDbContext>();
var commentsDbContext = serviceProvider.GetRequiredService<ICommentsDbContext>();
```

## Testing with Mongo2Go

NuGet-package: [https://www.nuget.org/packages/BrandUp.MongoDB.Testing.Mongo2Go/](https://www.nuget.org/packages/BrandUp.MongoDB.Testing.Mongo2Go/)

```
services.AddMongo2GoDbClientFactory();
```

## Testing with BrandUp.MongoDB.Testing

NuGet-package: [https://www.nuget.org/packages/BrandUp.MongoDB.Testing/](https://www.nuget.org/packages/BrandUp.MongoDB.Testing/)

```
services.AddFakeMongoDbClientFactory();
```