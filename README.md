# BrandUp.MongoDB

[![Build Status](https://dev.azure.com/brandup/BrandUp%20Core/_apis/build/status/BrandUp.Worker?branchName=master)](https://dev.azure.com/brandup/BrandUp%20Core/_build/latest?definitionId=14&branchName=master)

A thin, DI-friendly layer on top of the official `MongoDB.Driver` that gives you EF-style
database contexts, automatic collection registration, conventions, transactions, and
ergonomic test helpers.

## Installation

NuGet: [BrandUp.MongoDB](https://www.nuget.org/packages/BrandUp.MongoDB/)

```pwsh
dotnet add package BrandUp.MongoDB
```

## Define a context

Declare a class deriving from `MongoDbContext`. Each `IMongoCollection<TDocument>`
property is auto-registered as a collection. Mark every document with
`[MongoCollection]`.

```csharp
using BrandUp.MongoDB;
using MongoDB.Driver;

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
public class ArticleDocument { /* ... */ }

[MongoCollection(CollectionName = "Comments")]
public class CommentDocument { /* ... */ }
```

## Register with DI

```csharp
services.AddMongoDb(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
});

services
    .AddMongoDbContext<WebSiteDbContext>(options =>
    {
        options.DatabaseName = "WebSite";
    })
    .AddExtension<WebSiteDbContext, ICommentsDbContext>()
    .UseCamelCaseElementName()
    .UseIgnoreIfNull(true)
    .UseIgnoreIfDefault(false);
```

## Resolve and use

```csharp
var dbContext = serviceProvider.GetRequiredService<WebSiteDbContext>();
var commentsDbContext = serviceProvider.GetRequiredService<ICommentsDbContext>();

await dbContext.Articles.InsertOneAsync(new ArticleDocument { /* ... */ });
```

## Transactions (`await using`)

`MongoDbSession` is registered per DI scope. `ITransactionFactory` and
`IClientSessionHandle` are exposed alongside it. The transaction handle implements
both `IDisposable` and `IAsyncDisposable`, so prefer `await using` — that flows
the rollback path through `AbortTransactionAsync` instead of blocking the thread.

```csharp
using var scope = serviceProvider.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
var transactionFactory = scope.ServiceProvider.GetRequiredService<ITransactionFactory>();
var session = scope.ServiceProvider.GetRequiredService<IClientSessionHandle>();

await using var transaction = await transactionFactory.BeginAsync(ct);

await dbContext.Articles.InsertOneAsync(session, new ArticleDocument { /* ... */ }, cancellationToken: ct);

await transaction.CommitAsync(ct);
// If CommitAsync is not reached (exception, early return), DisposeAsync aborts the transaction.
```

## Per-collection configuration

Tweak `MongoCollectionSettings` or `CreateCollectionOptions` for a specific document type
without subclassing the metadata:

```csharp
services
    .AddMongoDbContext<WebSiteDbContext>(options => options.DatabaseName = "WebSite")
    .ConfigureCollection<ArticleDocument>(
        configureSettings: s => s.ReadPreference = ReadPreference.SecondaryPreferred,
        configureCreate:   c => c.Capped = false);
```

The `configureCreate` hook only fires the first time the context boots against a fresh
database — when the collection does not yet exist and is about to be created.

## Testing

### In-memory fakes — `BrandUp.MongoDB.Testing`

NuGet: [BrandUp.MongoDB.Testing](https://www.nuget.org/packages/BrandUp.MongoDB.Testing/)

```csharp
services.AddFakeMongoDb();
```

Fast and dependency-free; no MongoDB process required. Suitable when you only need
the in-memory shape of the driver API — many advanced operators (aggregation, change
streams, full filter pipelines) are intentionally not implemented.

### Real `mongod` — `BrandUp.MongoDB.Testing.EphemeralMongo`

NuGet: [BrandUp.MongoDB.Testing.EphemeralMongo](https://www.nuget.org/packages/BrandUp.MongoDB.Testing.EphemeralMongo/)

```csharp
services.AddEphemeralMongoDb();
```

Spins up a real ephemeral `mongod` (single-node replica set, so transactions work)
via [EphemeralMongo](https://github.com/asimmon/ephemeral-mongo). Pick this for
integration tests where you want the actual driver behaviour.

### Legacy — `BrandUp.MongoDB.Testing.Mongo2Go` (deprecated)

The Mongo2Go-backed helper is still published for backwards compatibility but is no
longer maintained upstream. Both `AddTestMongoDb()` and `Mongo2GoDbClientFactory`
are marked `[Obsolete]`; please migrate to `AddEphemeralMongoDb()`.
