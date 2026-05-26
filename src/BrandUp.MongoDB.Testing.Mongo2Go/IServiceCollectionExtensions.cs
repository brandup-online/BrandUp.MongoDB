using BrandUp.MongoDB.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    /// <summary>DI helpers backed by Mongo2Go. <b>Deprecated</b> — prefer <see cref="BrandUp.MongoDB.Testing"/>.EphemeralMongoDbClientFactory.</summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="Mongo2GoDbClientFactory"/> as the <see cref="IMongoDbClientFactory"/>.
        /// Deprecated; migrate to <c>AddEphemeralMongoDb</c> from the EphemeralMongo package.
        /// </summary>
        [Obsolete("Mongo2Go is no longer maintained. Migrate to BrandUp.MongoDB.Testing.EphemeralMongo and call AddEphemeralMongoDb instead.")]
        public static IServiceCollection AddTestMongoDb(this IServiceCollection services)
        {
            services.AddMongoDb();

#pragma warning disable CS0618
            return services.AddSingleton<IMongoDbClientFactory, Mongo2GoDbClientFactory>();
#pragma warning restore CS0618
        }
    }
}