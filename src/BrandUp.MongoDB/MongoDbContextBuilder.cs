using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrandUp.MongoDB
{
    public interface IMongoDbContextBuilder
    {
        Type DbContextType { get; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
        ConventionPack Conventions { get; }
        IEnumerable<MongoDbCollectionOptions> Collections { get; }
        IMongoDbClientFactory ClientFactory { get; set; }
        IMongoDbContextBuilder AddCollection(Type documentType);
        bool HasDocumentType(Type documentType);
        bool HasCollectionName(string name);
    }

    public class MongoDbContextBuilder<TContext> : IMongoDbContextBuilder
        where TContext : MongoDbContext
    {
        private static readonly object _initializationLock = new object();
        private readonly List<MongoDbCollectionOptions> collections = new List<MongoDbCollectionOptions>();
        private readonly Dictionary<Type, int> collectionDocumentTypes = new Dictionary<Type, int>();
        private readonly Dictionary<string, int> collectionNames = new Dictionary<string, int>();
        private readonly HashSet<Type> documentTypes = new HashSet<Type>();

        public MongoDbContextBuilder()
        {
            DbContextType = typeof(TContext);

            var properties = DbContextType.GetProperties();
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;
                if (!propertyType.IsConstructedGenericType)
                    continue;

                var doumentType = propertyType.GenericTypeArguments[0];

                AddCollection(doumentType);
            }
        }

        #region IMongoDbContextBuilder members

        public Type DbContextType { get; }
        public string ConnectionString { get; set; } = MongoDbDefaults.LocalConnectionString;
        public string DatabaseName { get; set; }
        public ConventionPack Conventions { get; } = new ConventionPack();
        public IEnumerable<MongoDbCollectionOptions> Collections => collections;
        public IMongoDbClientFactory ClientFactory { get; set; }
        public IMongoDbContextBuilder AddCollection(Type documentType)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));
            if (!documentType.IsClass)
                throw new ArgumentException("Document type require is class.");
            if (documentType.IsAbstract)
                throw new ArgumentException("Document type not allow abstract class.");

            if (collectionDocumentTypes.ContainsKey(documentType))
                throw new ArgumentException();

            var documentAttribute = documentType.GetCustomAttribute<DocumentAttribute>(false);
            if (documentAttribute == null)
                throw new ArgumentException($"Document type {documentType.AssemblyQualifiedName} not contain {nameof(DocumentAttribute)} attribute.");

            string collectionName = null;
            if (documentAttribute.CollectionName != null)
                collectionName = documentAttribute.CollectionName;
            else
                collectionName = TrimCollectionNamePrefix(documentType.Name);

            if (collectionNames.ContainsKey(collectionName.ToLower()))
                throw new InvalidOperationException();

            var index = collections.Count;
            collections.Add(new MongoDbCollectionOptions(collectionName, documentType) { ContextType = documentAttribute.CollectionContextType });
            collectionDocumentTypes.Add(documentType, index);
            collectionNames.Add(collectionName.ToLower(), index);

            AddDocumentType(documentType);

            return this;
        }
        public bool HasDocumentType(Type documentType)
        {
            return collectionDocumentTypes.ContainsKey(documentType);
        }
        public bool HasCollectionName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return collectionNames.ContainsKey(name.ToLower());
        }

        #endregion

        private MongoDbContextOptions BuildOptions()
        {
            if (ConnectionString == null)
                throw new InvalidOperationException($"Not set {nameof(ConnectionString)} value.");

            if (DatabaseName == null)
                throw new InvalidOperationException($"Not set {nameof(DatabaseName)} value.");

            var mongoUrlBuilder = new MongoUrlBuilder(ConnectionString)
            {
                DatabaseName = DatabaseName
            };

            var mongoUrl = mongoUrlBuilder.ToMongoUrl();

            var options = new MongoDbContextOptions
            {
                Url = mongoUrl,
                ClientFactory = ClientFactory ?? MongoDbClientFactory.Instance
            };

            options.Collections.AddRange(collections);

            return options;
        }

        public TContext Build(IServiceProvider provider)
        {
            var dbContextOptions = BuildOptions();
            var dbContextType = DbContextType;
            var dbContextName = dbContextType.FullName;

            lock (_initializationLock)
            {
                RegisterConventions(dbContextName);
            }

            var constructor = dbContextType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();

            var constructorParamsInfo = constructor.GetParameters();
            var constratorParams = new object[constructorParamsInfo.Length];
            var i = 0;
            foreach (var p in constructorParamsInfo)
            {
                if (p.ParameterType == typeof(MongoDbContextOptions))
                    constratorParams[i] = dbContextOptions;
                else
                    constratorParams[i] = provider.GetService(p.ParameterType);
                i++;
            }

            return (TContext)constructor.Invoke(constratorParams);
        }

        private void AddDocumentType(Type type)
        {
            if (documentTypes.Contains(type) || type == typeof(object))
                return;

            documentTypes.Add(type);

            foreach (var knownTypeAttribute in type.GetCustomAttributes<DocumentKnownTypeAttribute>(false))
            {
                if (!knownTypeAttribute.Type.IsSubclassOf(type))
                    throw new InvalidOperationException($"Type {knownTypeAttribute.Type.FullName} is not overide {type.FullName}.");

                AddDocumentType(knownTypeAttribute.Type);
            }

            AddDocumentType(type.BaseType);
        }
        private void RegisterConventions(string name)
        {
            ConventionRegistry.Register(name, Conventions, documentTypes.Contains);
        }

        public static string TrimCollectionNamePrefix(string name)
        {
            if (name.EndsWith("Document"))
                return name.Substring(0, name.Length - "Document".Length);
            return name;
        }
    }
}