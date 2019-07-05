using BrandUp.MongoDB;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, IConfiguration configuration)
            where TContext : MongoDbContext
        {
            return AddMongoDbContext<TContext>(services, configuration, null);
        }

        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, Action<IMongoDbContextBuilder> builderAction)
            where TContext : MongoDbContext
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var config = configuration.Get<MongoDbContextConfiguration>();

            services.AddMongoDbContext<TContext>(builder =>
            {
                builder.ConnectionString = config.ConnectionString;
                builder.DatabaseName = config.DatabaseName;

                if (config.CamelCase)
                    builder.UseCamelCaseElementName();

                builder.UseIgnoreIfDefault(config.IgnoreIfDefault);
                builder.UseIgnoreIfNull(config.IgnoreIfNull);

                builderAction?.Invoke(builder);
            });

            return services;
        }

        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, Action<IMongoDbContextBuilder> builderAction)
            where TContext : MongoDbContext
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));

            var builder = new MongoDbContextBuilder<TContext>();

            builderAction.Invoke(builder);

            services.AddSingleton(builder.Build);
            services.AddSingleton<IMongoDbClientFactory>(MongoDbClientFactory.Instance);

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