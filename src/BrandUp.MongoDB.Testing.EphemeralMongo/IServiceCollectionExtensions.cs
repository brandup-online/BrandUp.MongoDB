using BrandUp.MongoDB;
using BrandUp.MongoDB.Testing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EphemeralMongoServiceCollectionExtensions
    {
        public static IServiceCollection AddEphemeralMongoDb(this IServiceCollection services)
        {
            services.AddMongoDb();

            return services.AddSingleton<IMongoDbClientFactory, EphemeralMongoDbClientFactory>();
        }
    }
}
