using System;

namespace BrandUp.MongoDB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MongoDbDocumentAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public Type CollectionContextType { get; set; }
    }
}