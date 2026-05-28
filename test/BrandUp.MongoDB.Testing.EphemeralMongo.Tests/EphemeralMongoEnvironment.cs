using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests
{
    internal static class EphemeralMongoEnvironment
    {
        /// <summary>
        /// Resolves a service, skipping the test when EphemeralMongo cannot start its bundled mongod
        /// (e.g. on CI agents whose CPU lacks AVX, which MongoDB 5.0+ requires).
        /// </summary>
        public static T ResolveOrSkip<T>(IServiceProvider provider) where T : notnull
        {
            try
            {
                return provider.GetRequiredService<T>();
            }
            catch (global::EphemeralMongo.EphemeralMongoException ex)
            {
                Assert.Skip($"EphemeralMongo could not start mongod in this environment (MongoDB 5.0+ requires AVX): {ex.Message}");
                throw;
            }
        }
    }
}
