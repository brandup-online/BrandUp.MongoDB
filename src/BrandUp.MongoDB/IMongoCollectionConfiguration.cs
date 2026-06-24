using System.Reflection;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Implement this on a document type (or its base document) to declare collection parameters next to the
    /// document itself, instead of configuring them from application start-up. The configuration is discovered
    /// automatically when the collection is registered.
    /// <para>
    /// Because the member is <see langword="static"/>, the configuration is shared by every collection mapped to
    /// the type — declaring it on a base document applies it to all derived documents that map to a collection.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [MongoCollection(CollectionName = "events")]
    /// public class EventDocument : IMongoCollectionConfiguration
    /// {
    ///     public static void Configure(MongoCollectionConfigurationBuilder builder)
    ///     {
    ///         builder.Capped(maxSize: 16 * 1024 * 1024);
    ///         builder.Validation(new BsonDocument("$jsonSchema", ...));
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IMongoCollectionConfiguration
    {
        /// <summary>Declares the collection's parameters on the supplied <paramref name="builder"/>.</summary>
        static abstract void Configure(MongoCollectionConfigurationBuilder builder);
    }

    /// <summary>Invokes <see cref="IMongoCollectionConfiguration.Configure"/> for a document type known only by reflection.</summary>
    internal static class MongoCollectionConfigurationInvoker
    {
        static readonly MethodInfo invokeMethod = typeof(MongoCollectionConfigurationInvoker)
            .GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <summary>Applies the document's static configuration to <paramref name="builder"/>, if the type implements <see cref="IMongoCollectionConfiguration"/>.</summary>
        public static void Apply(Type documentType, MongoCollectionConfigurationBuilder builder)
        {
            if (!typeof(IMongoCollectionConfiguration).IsAssignableFrom(documentType))
                return;

            invokeMethod.MakeGenericMethod(documentType).Invoke(null, [builder]);
        }

        static void Invoke<TDocument>(MongoCollectionConfigurationBuilder builder)
            where TDocument : IMongoCollectionConfiguration
            => TDocument.Configure(builder);
    }
}
