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

        /// <summary>
        /// Creates the collection as capped. Requires <see cref="CappedMaxSize"/> to be set. The capped flag is
        /// applied only at creation; <see cref="CappedMaxSize"/>/<see cref="CappedMaxDocuments"/> can also be changed
        /// later on an existing capped collection (MongoDB 6.0+).
        /// <para>For validation rules — which cannot be expressed as a constant — implement <see cref="IMongoCollectionConfiguration"/> instead.</para>
        /// </summary>
        public bool Capped { get; set; }

        /// <summary>Maximum size in bytes for a capped collection. <c>0</c> means unset.</summary>
        public long CappedMaxSize { get; set; }

        /// <summary>Maximum document count for a capped collection. <c>0</c> means unset.</summary>
        public long CappedMaxDocuments { get; set; }

        /// <summary>Records change-stream pre/post images for the collection. Updatable on an existing collection.</summary>
        public bool ChangeStreamPreAndPostImages { get; set; }
    }
}