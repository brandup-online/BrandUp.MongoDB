using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.MongoDB
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddFakeMongoDbClientFactory(this IServiceCollection services)
        {
            return services.AddSingleton<IMongoDbClientFactory, Testing.FakeMongoDbClientFactory>();
        }
    }
}