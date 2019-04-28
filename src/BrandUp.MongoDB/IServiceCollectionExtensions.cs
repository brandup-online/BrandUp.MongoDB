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

        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, Action<IMongoDbContextBuilder> optionsAction)
            where TContext : MongoDbContext
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var config = configuration.Get<MongoDbContextConfiguration>();

            services.AddMongoDbContext<TContext>(options =>
            {
                options.ConnectionString = config.ConnectionString;
                options.DatabaseName = config.DatabaseName;

                if (config.CamelCase)
                    options.UseCamelCaseElementName();

                options.UseIgnoreIfDefault(config.IgnoreIfDefault);
                options.UseIgnoreIfNull(config.IgnoreIfNull);

                optionsAction?.Invoke(options);
            });

            return services;
        }

        public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, Action<IMongoDbContextBuilder> optionsAction)
            where TContext : MongoDbContext
        {
            if (optionsAction == null)
                throw new ArgumentNullException(nameof(optionsAction));

            var optionsBuilder = new MongoDbContextBuilder<TContext>();

            optionsAction.Invoke(optionsBuilder);

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