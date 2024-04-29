# BrandUp.MongoDB

[![Build Status](https://dev.azure.com/brandup/BrandUp%20Core/_apis/build/status/BrandUp.Worker?branchName=master)](https://dev.azure.com/brandup/BrandUp%20Core/_build/latest?definitionId=14&branchName=master)

## Installation

NuGet-package: [https://www.nuget.org/packages/BrandUp.MongoDB/](https://www.nuget.org/packages/BrandUp.MongoDB/)

## Configuration

```
public class WebSiteDbContext : MongoDbContext, ICommentsDbContext
{

	public IMongoCollection<ArticleDocument> Articles => GetCollection<ArticleDocument>();
	
	public IMongoCollection<CommentDocument> Comments => GetCollection<CommentDocument>();
}

public interface ICommentsDbContext
{
	IMongoCollection<CommentDocument> Comments { get; }
}

[MongoCollection(CollectionName = "Articles")]
public class ArticleDocument { }

[MongoCollection(CollectionName = "Comments")]
public class CommentDocument { }

// Configuration
services.AddMongoDb(options => { options.ConnectionString = "mongodb://localhost:27017"; });

services.AddMongoDbContext<WebSiteDbContext>(options =>
	{
		options.DatabaseName = "WebSite";
	})
	.AddExtension<WebSiteDbContext, ICommentsDbContext>()
	.UseCamelCaseElementName()
	.UseIgnoreIfNull(true)
	.UseIgnoreIfDefault(false);
```

## Using

```
var dbContext = serviceProvider.GetRequiredService<WebSiteDbContext>();
var commentsDbContext = serviceProvider.GetRequiredService<ICommentsDbContext>();
```

## Testing with Mongo2Go

NuGet-package: [https://www.nuget.org/packages/BrandUp.MongoDB.Testing.Mongo2Go/](https://www.nuget.org/packages/BrandUp.MongoDB.Testing.Mongo2Go/)

```
services.AddTestMongoDb();
```

## Testing with BrandUp.MongoDB.Testing

NuGet-package: [https://www.nuget.org/packages/BrandUp.MongoDB.Testing/](https://www.nuget.org/packages/BrandUp.MongoDB.Testing/)

```
services.AddFakeMongoDb();
```