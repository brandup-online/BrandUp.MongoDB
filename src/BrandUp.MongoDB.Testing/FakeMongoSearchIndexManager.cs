using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace BrandUp.MongoDB.Testing
{
    public class FakeMongoSearchIndexManager : IMongoSearchIndexManager
    {
        readonly Dictionary<string, BsonDocument> indexes = new();

        string CreateInternal(string name, BsonDocument? definition)
        {
            indexes[name.ToLower()] = new BsonDocument
            {
                { "name", name },
                { "definition", definition ?? new BsonDocument() }
            };

            return name;
        }

        public string CreateOne(CreateSearchIndexModel model, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            var name = model.Name ?? $"index{indexes.Count}";
            return CreateInternal(name, model.Definition);
        }

        public string CreateOne(BsonDocument definition, string? name = null, CancellationToken cancellationToken = default)
        {
            return CreateInternal(name ?? $"index{indexes.Count}", definition);
        }

        public IEnumerable<string> CreateMany(IEnumerable<CreateSearchIndexModel> models, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(models);

            return models.Select(model => CreateOne(model, cancellationToken)).ToList();
        }

        public Task<string> CreateOneAsync(CreateSearchIndexModel model, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateOne(model, cancellationToken));
        }

        public Task<string> CreateOneAsync(BsonDocument definition, string? name = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateOne(definition, name, cancellationToken));
        }

        public Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateSearchIndexModel> models, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateMany(models, cancellationToken));
        }

        public void DropOne(string name, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);

            indexes.Remove(name.ToLower());
        }

        public Task DropOneAsync(string name, CancellationToken cancellationToken = default)
        {
            DropOne(name, cancellationToken);

            return Task.CompletedTask;
        }

        public IAsyncCursor<BsonDocument> List(string name, CancellationToken cancellationToken = default)
        {
            var result = string.IsNullOrEmpty(name)
                ? indexes.Values
                : indexes.Where(kv => kv.Key == name.ToLower()).Select(kv => kv.Value);

            return new FakeAsyncCursor<BsonDocument>(result);
        }

        public IAsyncCursor<BsonDocument> List(string name, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            return List(name, cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(List(name, cancellationToken));
        }

        public Task<IAsyncCursor<BsonDocument>> ListAsync(string name, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(List(name, options, cancellationToken));
        }

        public void Update(string name, BsonDocument definition, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!indexes.TryGetValue(name.ToLower(), out var doc))
                throw new InvalidOperationException($"Search index \"{name}\" does not exist.");

            doc["definition"] = definition ?? new BsonDocument();
        }

        public Task UpdateAsync(string name, BsonDocument definition, CancellationToken cancellationToken = default)
        {
            Update(name, definition, cancellationToken);

            return Task.CompletedTask;
        }
    }
}
