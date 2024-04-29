using BrandUp.MongoDB.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddTestMongoDb(this IServiceCollection services)
        {
            services.AddMongoDb();

            return services.AddSingleton<IMongoDbClientFactory, Mongo2GoDbClientFactory>();
        }
    }
}