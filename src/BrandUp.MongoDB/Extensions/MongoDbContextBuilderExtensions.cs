using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>Fluent helpers for <see cref="IMongoDbContextBuilder"/>.</summary>
    public static class MongoDbContextBuilderExtensions
    {
        /// <summary>
        /// Registers an additional service alias that resolves to the same <typeparamref name="TContext"/> instance.
        /// Useful for exposing a context behind a narrow capability interface.
        /// </summary>
        public static MongoDbContextBuilder<TContext> AddExtension<TContext, TExtension>(this MongoDbContextBuilder<TContext> builder)
            where TContext : MongoDbContext, TExtension
            where TExtension : class
        {
            builder.Services.AddTransient<TExtension>(s => s.GetRequiredService<TContext>());

            return builder;
        }

        /// <summary>Registers an extra document type as a collection, beyond what was auto-discovered from the context properties.</summary>
        public static IMongoDbContextBuilder AddCollection<TDocument>(this IMongoDbContextBuilder builder)
            where TDocument : class
        {
            builder.RegisterCollection(typeof(TDocument));

            return builder;
        }

        /// <summary>Serialises element names in camelCase for all document types reachable from this context.</summary>
        public static IMongoDbContextBuilder UseCamelCaseElementName(this IMongoDbContextBuilder builder)
        {
            builder.Conventions.Add(new CamelCaseElementNameConvention());

            return builder;
        }

        /// <summary>Configures null-handling: when true, null property values are omitted from the BSON document.</summary>
        public static IMongoDbContextBuilder UseIgnoreIfNull(this IMongoDbContextBuilder builder, bool ignoreIfNull)
        {
            builder.Conventions.Add(new IgnoreIfNullConvention(ignoreIfNull));

            return builder;
        }

        /// <summary>Configures default-value handling: when true, default(T) property values are omitted from the BSON document.</summary>
        public static IMongoDbContextBuilder UseIgnoreIfDefault(this IMongoDbContextBuilder builder, bool ignoreIfDefault)
        {
            builder.Conventions.Add(new IgnoreIfDefaultConvention(ignoreIfDefault));

            return builder;
        }

        /// <summary>
        /// Customises the <see cref="MongoCollectionSettings"/> and/or <see cref="CreateCollectionOptions"/>
        /// used when the collection for <typeparamref name="TDocument"/> is resolved.
        /// </summary>
        /// <typeparam name="TDocument">The document type previously registered on the builder.</typeparam>
        /// <param name="builder">The context builder.</param>
        /// <param name="configureSettings">Optional hook applied to <see cref="MongoCollectionSettings"/> before the collection is fetched.</param>
        /// <param name="configureCreate">Optional hook applied to <see cref="CreateCollectionOptions"/> when the collection is missing and about to be created.</param>
        /// <exception cref="InvalidOperationException">Thrown if no collection for <typeparamref name="TDocument"/> has been registered.</exception>
        public static IMongoDbContextBuilder ConfigureCollection<TDocument>(
            this IMongoDbContextBuilder builder,
            Action<MongoCollectionSettings>? configureSettings = null,
            Action<CreateCollectionOptions>? configureCreate = null)
            where TDocument : class
        {
            var metadata = builder.Collections.OfType<MongoDbCollectionMetadata<TDocument>>().FirstOrDefault()
                ?? throw new InvalidOperationException($"Collection for document type {typeof(TDocument).FullName} is not registered.");

            if (configureSettings != null)
                metadata.ConfigureSettings = configureSettings;
            if (configureCreate != null)
                metadata.ConfigureCreate = configureCreate;

            return builder;
        }
    }
}
