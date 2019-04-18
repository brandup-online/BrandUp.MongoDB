using System;

namespace BrandUp.MongoDB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DocumentKnownTypeAttribute : Attribute
    {
        public Type Type { get; }

        public DocumentKnownTypeAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}