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
