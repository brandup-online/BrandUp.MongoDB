using BrandUp.MongoDB;
using BrandUp.MongoDB.Testing;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>DI helpers backed by EphemeralMongo's bundled <c>mongod</c> process.</summary>
    public static class EphemeralMongoServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="EphemeralMongoDbClientFactory"/> as the <see cref="IMongoDbClientFactory"/>.
        /// Each registration spins up its own ephemeral <c>mongod</c> for the lifetime of the service provider.
        /// </summary>
        public static IServiceCollection AddEphemeralMongoDb(this IServiceCollection services)
        {
            services.AddMongoDb();

            return services.AddSingleton<IMongoDbClientFactory, EphemeralMongoDbClientFactory>();
        }
    }
}
