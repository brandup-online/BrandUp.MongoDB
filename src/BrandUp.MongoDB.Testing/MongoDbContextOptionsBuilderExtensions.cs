namespace BrandUp.MongoDB
{
    public static class MongoDbContextOptionsBuilderExtensions
    {
        public static IMongoDbContextBuilder UseFakeClientFactory(this IMongoDbContextBuilder builder)
        {
            builder.ClientFactory = new Testing.FakeMongoDbClientFactory();

            return builder;
        }
    }
}