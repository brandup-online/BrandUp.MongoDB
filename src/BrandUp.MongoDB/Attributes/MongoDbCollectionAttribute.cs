using System;

namespace BrandUp.MongoDB
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MongoDbCollectionAttribute : Attribute
    {
        public string Name { get; set; }
        public Type Provider { get; set; }
    }
}