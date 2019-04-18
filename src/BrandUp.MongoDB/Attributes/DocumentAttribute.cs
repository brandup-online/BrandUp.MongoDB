using System;

namespace BrandUp.MongoDB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DocumentAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public Type CollectionContextType { get; set; }
    }
}