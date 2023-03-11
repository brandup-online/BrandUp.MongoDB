using System;
using BrandUp.MongoDB;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IMongoDbContextBuilder AddMongoDbContext<TContext>(this IServiceCollection services, Action<MongoDbContextConfiguration> configAction)
            where TContext : MongoDbContext
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction));

            var builder = new MongoDbContextBuilder<TContext>();

            services.AddSingleton(builder.Build);

            return builder;
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