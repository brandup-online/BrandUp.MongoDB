using BrandUp.MongoDB.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddFakeMongoDb(this IServiceCollection services)
        {
            return services.AddSingleton<IMongoDbClientFactory, FakeMongoDbClientFactory>();
        }
    }
}