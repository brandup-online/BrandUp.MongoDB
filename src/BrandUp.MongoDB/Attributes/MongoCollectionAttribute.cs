using System;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Marks a class as a MongoDB document so it can be registered as a collection on a
    /// <see cref="MongoDbContext"/>. Required on every document type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class MongoCollectionAttribute : Attribute
    {
        /// <summary>
        /// Optional collection name. When omitted, the name is derived from the document class name
        /// with any trailing <c>Document</c> or <c>Model</c> suffix stripped.
        /// </summary>
        public string? CollectionName { get; set; }
    }
}