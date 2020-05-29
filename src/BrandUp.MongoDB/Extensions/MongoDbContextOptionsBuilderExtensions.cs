using MongoDB.Bson.Serialization.Conventions;

namespace BrandUp.MongoDB
{
    public static class MongoDbContextOptionsBuilderExtensions
    {
        public static IMongoDbContextBuilder AddCollection<TDocument>(this IMongoDbContextBuilder builder)
            where TDocument : class
        {
            builder.AddCollection(typeof(TDocument));

            return builder;
        }

        public static IMongoDbContextBuilder UseCamelCaseElementName(this IMongoDbContextBuilder builder)
        {
            builder.Conventions.Add(new CamelCaseElementNameConvention());

            return builder;
        }

        public static IMongoDbContextBuilder UseIgnoreIfNull(this IMongoDbContextBuilder builder, bool ignoreIfNull)
        {
            builder.Conventions.Add(new IgnoreIfNullConvention(ignoreIfNull));

            return builder;
        }

        public static IMongoDbContextBuilder UseIgnoreIfDefault(this IMongoDbContextBuilder builder, bool ignoreIfDefault)
        {
            builder.Conventions.Add(new IgnoreIfDefaultConvention(ignoreIfDefault));

            return builder;
        }
    }
}