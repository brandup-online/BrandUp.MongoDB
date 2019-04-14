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

        public static IServiceCollection AddMongoDbContextExension<TContext, TExtension>(this IServiceCollection services)
            where TContext : MongoDbContext, TExtension
            where TExtension : class
        {
            services.AddTransient<TExtension>(s => s.GetService<TContext>());
            return services;
        }
    }
}