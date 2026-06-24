using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>Per-context configuration. <see cref="DatabaseName"/> is required.</summary>
    public class MongoDbContextOptions
    {
        /// <summary>Name of the MongoDB database backing this context. Required.</summary>
        public string? DatabaseName { get; set; }

        /// <summary>Optional <see cref="MongoDatabaseSettings"/> passed when resolving the database handle.</summary>
        public MongoDatabaseSettings? DatabaseSettings { get; set; }

        /// <summary>
        /// When true, collection parameters declared via <see cref="MongoCollectionAttribute"/> or
        /// <see cref="IMongoCollectionConfiguration"/> are reconciled onto already-existing collections during
        /// initialization (via the <c>collMod</c> command). Only fast, metadata-only changes are applied —
        /// document validation, capped size, and change-stream pre/post images; immutable options and indexes
        /// are never touched. Disabled by default to avoid unexpected schema changes against a live database.
        /// </summary>
        public bool UpdateExistingCollections { get; set; }
    }

    /// <summary>Validates that <see cref="MongoDbContextOptions.DatabaseName"/> is set.</summary>
    public class MongoDbContextOptionsValidator : IValidateOptions<MongoDbContextOptions>
    {
        /// <inheritdoc />
        public ValidateOptionsResult Validate(string? name, MongoDbContextOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.DatabaseName))
                return ValidateOptionsResult.Fail($"Parameter {nameof(MongoDbContextOptions.DatabaseName)} is required.");

            return ValidateOptionsResult.Success;
        }
    }
}
