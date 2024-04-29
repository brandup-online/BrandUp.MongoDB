using System;
using BrandUp.MongoDB;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoDb(this IServiceCollection services, Action<MongoDbOptions> configure)
        {
            services
                .AddOptions<MongoDbOptions>()
                .Configure(configure)
                .Validate(options => !string.IsNullOrEmpty(options.ConnectionString), $"Paramenter {nameof(MongoDbOptions.ConnectionString)} is required.");

            services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

            return services;
        }

        public static MongoDbContextBuilder<TContext> AddMongoDbContext<TContext>(this IServiceCollection services, Action<MongoDbContextOptions> configureOptions)
            where TContext : MongoDbContext
        {
            ArgumentNullException.ThrowIfNull(configureOptions);

            var contextType = typeof(TContext);

            services
                .AddOptions<MongoDbContextOptions>(contextType.FullName)
                .Configure(configureOptions);

            services.AddSingleton<IValidateOptions<MongoDbContextOptions>, MongoDbContextOptionsValidator>();

            var builder = new MongoDbContextBuilder<TContext>(services);

            services.AddSingleton(builder.Build);

            return builder;
        }

        public static IServiceCollection ConfigureMongoDbContext<TContext>(this IServiceCollection services, Action<MongoDbContextOptions> configureOptions)
            where TContext : MongoDbContext
        {
            ArgumentNullException.ThrowIfNull(configureOptions);

            var contextType = typeof(TContext);

            services.Configure(contextType.FullName, configureOptions);

            return services;
        }
    }
}