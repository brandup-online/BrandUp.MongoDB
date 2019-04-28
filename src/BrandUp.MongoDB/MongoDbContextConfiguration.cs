namespace BrandUp.MongoDB
{
    public class MongoDbContextConfiguration
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public bool CamelCase { get; set; }
        public bool IgnoreIfNull { get; set; }
        public bool IgnoreIfDefault { get; set; }
    }
}