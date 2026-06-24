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

## Collection parameters

Collection parameters can be declared right where the collection is registered — on the
document — instead of being scattered across application start-up. Two complementary ways:

### 1. On the `[MongoCollection]` attribute (constants)

```csharp
[MongoCollection(CollectionName = "events",
    Capped = true, CappedMaxSize = 16 * 1024 * 1024, CappedMaxDocuments = 100_000,
    ChangeStreamPreAndPostImages = true)]
public class EventDocument { /* ... */ }
```

### 2. On the document via `IMongoCollectionConfiguration` (full, programmatic)

Implement the interface to express anything the attribute can't — notably document
validation. Declaring it on a *base* document applies the configuration to every derived
document mapped to a collection.

```csharp
[MongoCollection(CollectionName = "people")]
public class PersonDocument : IMongoCollectionConfiguration
{
    [BsonElement("name")] public string? Name { get; set; }

    public static void Configure(MongoCollectionConfigurationBuilder builder)
    {
        builder.Capped(maxSize: 16 * 1024 * 1024);
        builder.Validation(
            new BsonDocument("$jsonSchema", new BsonDocument
            {
                { "bsonType", "object" },
                { "required", new BsonArray { "name" } }
            }),
            DocumentValidationLevel.Strict,
            DocumentValidationAction.Error);
        builder.ChangeStreamPreAndPostImages();
    }
}
```

Only parameters MongoDB can change on an existing collection **quickly** (metadata-only,
no scan or rewrite) are exposed: document validation, capped size/max, and change-stream
pre/post images. Immutable options (collation, the capped flag itself, clustered index)
and indexes — including TTL — are deliberately out of scope.

### Updating already-existing collections

By default parameters are applied only when a collection is **created**. Enable
`UpdateExistingCollections` to also reconcile declared parameters onto an existing
collection (via the `collMod` command) when the context initializes. Only fields that
actually differ are sent, so it is a no-op when nothing changed.

```csharp
services.AddMongoDbContext<WebSiteDbContext>(options =>
{
    options.DatabaseName = "WebSite";
    options.UpdateExistingCollections = true;
});
```

> Disabled by default to avoid unexpected schema changes against a live database. Note that
> MongoDB rounds a capped collection's size up to a multiple of 256 bytes — declare sizes on
> that boundary to avoid a harmless `collMod` resize on every start-up.

### Escape hatch — raw driver options from DI

For environment-specific tweaks you can still reach the raw driver options. These hooks run
*after* the declared parameters, so they win on conflict. `configureCreate` only fires when
the collection is about to be created.

```csharp
services
    .AddMongoDbContext<WebSiteDbContext>(options => options.DatabaseName = "WebSite")
    .ConfigureCollection<ArticleDocument>(
        configureSettings: s => s.ReadPreference = ReadPreference.SecondaryPreferred,
        configureCreate:   c => c.Capped = false);
```

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
