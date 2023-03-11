using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddTestMongoDb(this IServiceCollection services)
        {
            return services.AddSingleton<IMongoDbClientFactory, Testing.Mongo2GoDbClientFactory>();
        }
    }
}