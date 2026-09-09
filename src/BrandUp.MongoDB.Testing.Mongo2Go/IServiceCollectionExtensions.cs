using BrandUp.MongoDB.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    /// <summary>DI helpers backed by Mongo2Go.</summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="Mongo2GoDbClientFactory"/> as the <see cref="IMongoDbClientFactory"/>.
        /// </summary>
        public static IServiceCollection AddTestMongoDb(this IServiceCollection services)
        {
            services.AddMongoDb();

            return services.AddSingleton<IMongoDbClientFactory, Mongo2GoDbClientFactory>();
        }
    }
}