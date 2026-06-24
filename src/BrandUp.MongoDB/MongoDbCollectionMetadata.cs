using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Per-document-type registration on a <see cref="MongoDbContext"/>. Holds the collection name
    /// derived from <see cref="MongoCollectionAttribute"/>, the resolved <see cref="IMongoCollection{TDocument}"/>,
    /// the declared collection <see cref="Configuration"/>, and optional configuration hooks applied during context initialization.
    /// </summary>
    public class MongoDbCollectionMetadata<TDocument> : IMongoDbCollectionMetadata
        where TDocument : class
    {
        /// <summary>The resolved collection handle. Available after the owning context has been initialized.</summary>
        public IMongoCollection<TDocument> Collection { get; private set; } = null!;

        /// <summary>
        /// Collection parameters declared via <see cref="MongoCollectionAttribute"/> and
        /// <see cref="IMongoCollectionConfiguration"/>. Applied when the collection is created and, when the owning
        /// context has <see cref="MongoDbContextOptions.UpdateExistingCollections"/> enabled, reconciled onto an
        /// existing collection.
        /// </summary>
        public MongoCollectionConfigurationBuilder Configuration { get; } = new();

        /// <summary>
        /// Hook applied to <see cref="CreateCollectionOptions"/> just before the
        /// collection is created (only when the collection does not yet exist).
        /// Runs after <see cref="Configuration"/> so it can override declared parameters.
        /// </summary>
        public Action<CreateCollectionOptions>? ConfigureCreate { get; set; }

        /// <summary>
        /// Hook applied to <see cref="MongoCollectionSettings"/> before the
        /// driver hands out the <see cref="IMongoCollection{TDocument}"/>.
        /// </summary>
        public Action<MongoCollectionSettings>? ConfigureSettings { get; set; }

        internal MongoDbCollectionMetadata()
        {
            var documentType = DocumentType;

            var collectionAttribute = documentType.GetCustomAttribute<MongoCollectionAttribute>(false) ?? throw new ArgumentException($"Document type {documentType.AssemblyQualifiedName} not contain {nameof(MongoCollectionAttribute)} attribute.");

            string collectionName;
            if (collectionAttribute.CollectionName != null)
                collectionName = collectionAttribute.CollectionName;
            else
                collectionName = TrimCollectionNamePrefix(documentType.Name);

            Name = collectionName;

            ApplyDeclaredConfiguration(collectionAttribute);
        }

        void ApplyDeclaredConfiguration(MongoCollectionAttribute attribute)
        {
            // Option 1: simple, constant parameters declared on the attribute.
            if (attribute.Capped)
            {
                if (attribute.CappedMaxSize <= 0)
                    throw new ArgumentException($"{nameof(MongoCollectionAttribute.CappedMaxSize)} must be positive when {nameof(MongoCollectionAttribute.Capped)} is set on {DocumentType.FullName}.");

                Configuration.Capped(attribute.CappedMaxSize, attribute.CappedMaxDocuments > 0 ? attribute.CappedMaxDocuments : null);
            }
            else if (attribute.CappedMaxSize > 0 || attribute.CappedMaxDocuments > 0)
                throw new ArgumentException($"{nameof(MongoCollectionAttribute.CappedMaxSize)}/{nameof(MongoCollectionAttribute.CappedMaxDocuments)} require {nameof(MongoCollectionAttribute.Capped)} = true on {DocumentType.FullName}.");

            if (attribute.ChangeStreamPreAndPostImages)
                Configuration.ChangeStreamPreAndPostImages(true);

            // Option 2: full, programmatic configuration declared on the document type (or its base document).
            MongoCollectionConfigurationInvoker.Apply(DocumentType, Configuration);
        }

        #region IMongoDbCollectionContext members

        public string Name { get; }
        public Type DocumentType { get; } = typeof(TDocument);
        void IMongoDbCollectionMetadata.Initialize(MongoDbContext dbContext, bool updateExisting, CancellationToken cancellationToken)
        {
            var collectionNames = dbContext.Database.ListCollectionNames(cancellationToken: cancellationToken).ToList(cancellationToken);
            var exists = collectionNames.Any(name => name.Equals(Name, StringComparison.InvariantCultureIgnoreCase));
            if (!exists)
            {
                var createOptions = BuildCreateOptions();
                ConfigureCreate?.Invoke(createOptions);
                dbContext.Database.CreateCollection(Name, createOptions, cancellationToken);
            }
            else if (updateExisting)
                ReconcileExisting(dbContext.Database, cancellationToken);

            var collectionSettings = new MongoCollectionSettings();
            ConfigureSettings?.Invoke(collectionSettings);
            Collection = dbContext.Database.GetCollection<TDocument>(Name, collectionSettings);
        }

        #endregion

        CreateCollectionOptions<TDocument> BuildCreateOptions()
        {
            var options = new CreateCollectionOptions<TDocument>();

            if (Configuration.IsCapped)
            {
                options.Capped = true;
                options.MaxSize = Configuration.MaxSize;
                if (Configuration.MaxDocuments.HasValue)
                    options.MaxDocuments = Configuration.MaxDocuments;
            }

            if (Configuration.Validator != null)
                options.Validator = Configuration.Validator;
            if (Configuration.ValidationLevel.HasValue)
                options.ValidationLevel = Configuration.ValidationLevel;
            if (Configuration.ValidationAction.HasValue)
                options.ValidationAction = Configuration.ValidationAction;

            if (Configuration.ChangeStreamPreAndPostImagesEnabled.HasValue)
                options.ChangeStreamPreAndPostImagesOptions = new ChangeStreamPreAndPostImagesOptions { Enabled = Configuration.ChangeStreamPreAndPostImagesEnabled.Value };

            return options;
        }

        /// <summary>
        /// Applies the declared parameters that MongoDB can change quickly on an existing collection via
        /// <c>collMod</c> (validation, capped size, change-stream pre/post images). Only fields that actually
        /// differ from the current options are sent, so this is a no-op when nothing changed.
        /// </summary>
        void ReconcileExisting(IMongoDatabase database, CancellationToken cancellationToken)
        {
            if (!Configuration.HasUpdatableParameters)
                return;

            var current = GetCurrentOptions(database, cancellationToken);
            var command = new BsonDocument { { "collMod", Name } };

            if (Configuration.Validator != null && !Configuration.Validator.Equals(current.GetValue("validator", BsonNull.Value)))
                command["validator"] = Configuration.Validator;

            if (Configuration.ValidationLevel.HasValue)
            {
                var level = MapValidationLevel(Configuration.ValidationLevel.Value);
                if (AsStringOrNull(current, "validationLevel") != level)
                    command["validationLevel"] = level;
            }

            if (Configuration.ValidationAction.HasValue)
            {
                var action = MapValidationAction(Configuration.ValidationAction.Value);
                if (AsStringOrNull(current, "validationAction") != action)
                    command["validationAction"] = action;
            }

            // Capped resize only applies to a collection that is already capped; the capped flag itself cannot
            // be changed quickly and is therefore never reconciled.
            if (current.GetValue("capped", BsonBoolean.False).ToBoolean())
            {
                if (Configuration.MaxSize.HasValue && AsInt64OrNull(current, "size") != Configuration.MaxSize.Value)
                    command["cappedSize"] = Configuration.MaxSize.Value;
                if (Configuration.MaxDocuments.HasValue && AsInt64OrNull(current, "max") != Configuration.MaxDocuments.Value)
                    command["cappedMax"] = Configuration.MaxDocuments.Value;
            }

            if (Configuration.ChangeStreamPreAndPostImagesEnabled.HasValue)
            {
                var currentEnabled = current.GetValue("changeStreamPreAndPostImages", new BsonDocument()).AsBsonDocument.GetValue("enabled", BsonBoolean.False).ToBoolean();
                if (currentEnabled != Configuration.ChangeStreamPreAndPostImagesEnabled.Value)
                    command["changeStreamPreAndPostImages"] = new BsonDocument("enabled", Configuration.ChangeStreamPreAndPostImagesEnabled.Value);
            }

            if (command.ElementCount == 1) // only the "collMod" element: nothing differs
                return;

            database.RunCommand<BsonDocument>(command, cancellationToken: cancellationToken);
        }

        BsonDocument GetCurrentOptions(IMongoDatabase database, CancellationToken cancellationToken)
        {
            var listOptions = new ListCollectionsOptions { Filter = Builders<BsonDocument>.Filter.Eq("name", Name) };
            var description = database.ListCollections(listOptions, cancellationToken).ToList(cancellationToken).FirstOrDefault();
            return description != null && description.TryGetValue("options", out var options) && options.IsBsonDocument
                ? options.AsBsonDocument
                : new BsonDocument();
        }

        static string? AsStringOrNull(BsonDocument document, string name)
            => document.TryGetValue(name, out var value) && value.IsString ? value.AsString : null;

        static long? AsInt64OrNull(BsonDocument document, string name)
            => document.TryGetValue(name, out var value) && value.IsNumeric ? value.ToInt64() : null;

        static string MapValidationLevel(DocumentValidationLevel level) => level switch
        {
            DocumentValidationLevel.Off => "off",
            DocumentValidationLevel.Strict => "strict",
            DocumentValidationLevel.Moderate => "moderate",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

        static string MapValidationAction(DocumentValidationAction action) => action switch
        {
            DocumentValidationAction.Error => "error",
            DocumentValidationAction.Warn => "warn",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        public static string TrimCollectionNamePrefix(string name)
        {
            if (name.EndsWith("Document"))
                return name[..^"Document".Length];
            else if (name.EndsWith("Model"))
                return name[..^"Model".Length];
            return name;
        }
    }

    /// <summary>Untyped collection registration on a <see cref="MongoDbContext"/>.</summary>
    public interface IMongoDbCollectionMetadata
    {
        /// <summary>Collection name in the MongoDB database.</summary>
        string Name { get; }

        /// <summary>The .NET type of the documents stored in this collection.</summary>
        Type DocumentType { get; }

        /// <summary>
        /// Resolves the underlying <see cref="IMongoCollection{TDocument}"/> handle. Invoked once by the owning context.
        /// </summary>
        /// <param name="dbContext">The owning context.</param>
        /// <param name="updateExisting">When true, declared parameters are reconciled onto an already-existing collection via <c>collMod</c>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        void Initialize(MongoDbContext dbContext, bool updateExisting = false, CancellationToken cancellationToken = default);
    }
}
