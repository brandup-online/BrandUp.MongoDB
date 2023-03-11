using Microsoft.Extensions.Options;

namespace BrandUp.MongoDB
{
    public class MongoDbContextOptions
    {
        public string ConnectionString { get; set; } = MongoDbDefaults.LocalConnectionString;
        public string DatabaseName { get; set; }
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