using System;
using BrandUp.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>DI registration helpers for <see cref="BrandUp.MongoDB"/>.</summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the shared <see cref="IMongoDbClientFactory"/>, scoped <see cref="MongoDbSession"/>,
        /// and an <see cref="ITransactionFactory"/> alias. Call once per <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration of the global <see cref="MongoDbOptions"/>.</param>
        public static IServiceCollection AddMongoDb(this IServiceCollection services, Action<MongoDbOptions>? configure = null)
        {
            var optionsBuilder = services
                .AddOptions<MongoDbOptions>()
                .Validate(options => !string.IsNullOrEmpty(options.ConnectionString), $"Parameter {nameof(MongoDbOptions.ConnectionString)} is required.");

            if (configure != null)
                optionsBuilder.Configure(configure);

            services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

            services
                .AddScoped<MongoDbSession>()
                .AddTransient<ITransactionFactory>((s) => s.GetRequiredService<MongoDbSession>())
                .AddTransient((s) => s.GetRequiredService<MongoDbSession>().Current);

            return services;
        }

        /// <summary>
        /// Registers a <see cref="MongoDbContext"/> subclass as a singleton, discovers its
        /// <see cref="IMongoCollection{TDocument}"/> properties, and returns a builder for further configuration.
        /// </summary>
        /// <typeparam name="TContext">The concrete context type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures <see cref="MongoDbContextOptions"/> for this context.</param>
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

        /// <summary>
        /// Adds an additional configuration step for an already-registered <see cref="MongoDbContext"/>.
        /// Call this after <see cref="AddMongoDbContext{TContext}"/> to layer extra options.
        /// </summary>
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