using BrandUp.MongoDB.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    /// <summary>DI helpers for the in-memory <see cref="FakeMongoDbClientFactory"/> used in unit tests.</summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="FakeMongoDbClientFactory"/> as the <see cref="IMongoDbClientFactory"/>.
        /// Use for fast unit tests that don't need a real MongoDB process.
        /// </summary>
        public static IServiceCollection AddFakeMongoDb(this IServiceCollection services)
        {
            return services.AddSingleton<IMongoDbClientFactory, FakeMongoDbClientFactory>();
        }
    }
}