using BrandUp.MongoDB.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    public static class IServiceCollectionExtensions
    {
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