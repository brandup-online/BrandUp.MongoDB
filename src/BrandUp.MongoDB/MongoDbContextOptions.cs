using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public class MongoDbContextOptions
    {
        public string DatabaseName { get; set; }
        public MongoDatabaseSettings DatabaseSettings { get; set; }
    }

    public class MongoDbContextOptionsValidator : IValidateOptions<MongoDbContextOptions>
    {
        public ValidateOptionsResult Validate(string name, MongoDbContextOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.DatabaseName))
                return ValidateOptionsResult.Fail($"Paramenter {nameof(MongoDbContextOptions.DatabaseName)} is required.");

            return ValidateOptionsResult.Success;
        }
    }
}