using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public interface IMongoDbContextBuilder
    {
        IServiceCollection Services { get; }
        Type ContextType { get; }
        ConventionPack Conventions { get; }
        IEnumerable<IMongoDbCollectionMetadata> Collections { get; }
        IMongoDbContextBuilder RegisterCollection(Type documentType);
        bool HasCollectionType(Type documentType);
        bool HasCollectionName(string name);
    }

    public class MongoDbContextBuilder<TContext> : IMongoDbContextBuilder
        where TContext : MongoDbContext
    {
        readonly static Type MongoCollectionType = typeof(IMongoCollection<>);
        readonly static Type MongoCollectionMetadataType = typeof(MongoDbCollectionMetadata<>);

        readonly List<IMongoDbCollectionMetadata> collections = [];
        readonly Dictionary<Type, int> collectionTypes = [];
        readonly Dictionary<string, int> collectionNames = [];
        readonly HashSet<Type> documentTypes = [];
        TContext dbContext;

        public MongoDbContextBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            ContextType = typeof(TContext);

            AddContextCollections();
        }

        #region Helpers

        void AddContextCollections()
        {
            var properties = ContextType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;
                if (!propertyType.IsConstructedGenericType)
                    continue;
                if (propertyType.GetGenericTypeDefinition() != MongoCollectionType)
                    continue;

                var documentType = propertyType.GenericTypeArguments[0];

                RegisterCollection(documentType);
            }
        }

        void AddDocumentType(Type type)
        {
            if (documentTypes.Contains(type) || type == typeof(object))
                return;

            documentTypes.Add(type);

            foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty))
            {
                var propertyType = propertyInfo.PropertyType;
                var code = Type.GetTypeCode(propertyType);
                switch (code)
                {
                    case TypeCode.Object:
                        {
                            if (propertyType.IsGenericType)
                            {
                                if (typeof(IEnumerable).IsAssignableFrom(propertyType) || propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    AddDocumentType(propertyType.GenericTypeArguments[0]);
                                    break;
                                }
                            }
                            else if (propertyType.IsArray)
                            {
                                AddDocumentType(propertyType.GetElementType());
                                break;
                            }

                            AddDocumentType(propertyType);

                            break;
                        }
                }
            }

            foreach (var knownTypeAttribute in type.GetCustomAttributes<BsonKnownTypesAttribute>(false))
            {
                foreach (var knownType in knownTypeAttribute.KnownTypes)
                {
                    if (!knownType.IsSubclassOf(type))
                        throw new InvalidOperationException($"Type {knownType.FullName} is not overide {type.FullName}.");

                    AddDocumentType(knownType);
                }

            }

            foreach (var knownTypeAttribute in type.GetCustomAttributes<KnownTypeAttribute>(false))
            {
                if (!knownTypeAttribute.Type.IsSubclassOf(type))
                    throw new InvalidOperationException($"Type {knownTypeAttribute.Type.FullName} is not overide {type.FullName}.");

                AddDocumentType(knownTypeAttribute.Type);
            }

            AddDocumentType(type.BaseType);
        }

        void RegisterConventions(string name)
        {
            ConventionRegistry.Remove(name);
            ConventionRegistry.Register(name, Conventions, documentTypes.Contains);
        }

        #endregion

        #region IMongoDbContextBuilder members

        public IServiceCollection Services { get; }
        public Type ContextType { get; }
        public ConventionPack Conventions { get; } = [];
        public IEnumerable<IMongoDbCollectionMetadata> Collections => collections;
        public IMongoDbContextBuilder RegisterCollection(Type documentType)
        {
            ArgumentNullException.ThrowIfNull(documentType);
            if (!documentType.IsClass)
                throw new ArgumentException("Document type require is class.");

            if (collectionTypes.ContainsKey(documentType))
                throw new ArgumentException($"Collection document type {documentType.AssemblyQualifiedName} already registered.");

            var documentAttribute = documentType.GetCustomAttribute<MongoCollectionAttribute>(false);
            if (documentAttribute == null)
                throw new ArgumentException($"Document type {documentType.AssemblyQualifiedName} not contain {nameof(MongoCollectionAttribute)} attribute.");

            var collectionMetadataType = MongoCollectionMetadataType.MakeGenericType(documentType);
            var collectionMetadata = (IMongoDbCollectionMetadata)Activator.CreateInstance(collectionMetadataType, false);

            var index = collections.Count;
            collections.Add(collectionMetadata);
            collectionTypes.Add(documentType, index);
            collectionNames.Add(collectionMetadata.Name.ToLower(), index);

            AddDocumentType(documentType);

            return this;
        }
        public bool HasCollectionType(Type documentType)
        {
            return collectionTypes.ContainsKey(documentType);
        }
        public bool HasDocumentType(Type documentType)
        {
            return documentTypes.Contains(documentType);
        }
        public bool HasCollectionName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return collectionNames.ContainsKey(name.ToLower());
        }

        #endregion

        public TContext Build(IServiceProvider serviceProvider)
        {
            if (dbContext != null)
                return dbContext;

            var dbContextName = ContextType.FullName;

            RegisterConventions(dbContextName);

            var constructor = ContextType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();

            var constructorParamsInfo = constructor.GetParameters();
            var constratorParams = new object[constructorParamsInfo.Length];
            for (var i = 0; i < constructorParamsInfo.Length; i++)
            {
                var parameter = constructorParamsInfo[i];
                var service = serviceProvider.GetRequiredService(parameter.ParameterType);
                constratorParams[i] = service;

                i++;
            }

            dbContext = (TContext)constructor.Invoke(constratorParams);
            dbContext.Initialize(serviceProvider, collections);

            return dbContext;
        }
    }
}