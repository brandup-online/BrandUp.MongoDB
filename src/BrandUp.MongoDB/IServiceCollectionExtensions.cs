using BrandUp.MongoDB;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services)
            where TContext : MongoDbContext
        {
            services.AddMongoDbContext<TContext>(null);

            return services;
        }

        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, Action<IMongoDbContextBuilder> optionsAction)
            where TContext : MongoDbContext
        {
            var optionsBuilder = new MongoDbContextBuilder<TContext>();

            optionsAction?.Invoke(optionsBuilder);

            services.AddSingleton(s =>
            {
                return optionsBuilder.Build();
            });

            return services;
        }

        public static IServiceCollection AddMongoDbContextImplementation<TContextImplementation, TContext>(this IServiceCollection services)
            where TContextImplementation : class
            where TContext : MongoDbContext, TContextImplementation
        {
            services.AddSingleton<TContextImplementation>(s => s.GetService<TContext>());
            return services;
        }
    }
}