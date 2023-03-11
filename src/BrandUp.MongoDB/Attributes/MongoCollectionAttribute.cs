using System;

namespace BrandUp.MongoDB
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class MongoCollectionAttribute : Attribute
    {
        public string CollectionName { get; set; }
    }
}