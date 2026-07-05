using MongoDB.Bson;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// Declarative, strongly-typed description of the parameters a collection should have. Populated from
    /// <see cref="MongoCollectionAttribute"/> and <see cref="IMongoCollectionConfiguration.Configure"/>, then
    /// applied both when the collection is created and (optionally) reconciled onto an existing collection.
    /// <para>
    /// Most parameters exposed here are ones MongoDB can change on an existing collection quickly (metadata-only,
    /// no full scan or rewrite): document validation, capped size/max, and change-stream pre/post images. A few
    /// create-only options that are still expressible as simple constants are also exposed — the capped flag and
    /// the WiredTiger block compressor — and are applied only when the collection is created, never reconciled.
    /// Remaining immutable options (collation, clustered index) and indexes (including TTL) are out of scope.
    /// </para>
    /// </summary>
    public sealed class MongoCollectionConfigurationBuilder
    {
        /// <summary>Whether the collection should be created as capped. Applied only at creation time.</summary>
        public bool IsCapped { get; private set; }

        /// <summary>Maximum size in bytes for a capped collection. Updatable on an existing capped collection (MongoDB 6.0+).</summary>
        public long? MaxSize { get; private set; }

        /// <summary>Maximum document count for a capped collection. Updatable on an existing capped collection (MongoDB 6.0+).</summary>
        public long? MaxDocuments { get; private set; }

        /// <summary>Document validation expression (e.g. a <c>$jsonSchema</c>). Updatable on an existing collection.</summary>
        public BsonDocument? Validator { get; private set; }

        /// <summary>Validation strictness. Updatable on an existing collection.</summary>
        public DocumentValidationLevel? ValidationLevel { get; private set; }

        /// <summary>Action taken when a document fails validation. Updatable on an existing collection.</summary>
        public DocumentValidationAction? ValidationAction { get; private set; }

        /// <summary>Whether change-stream pre/post images are recorded. Updatable on an existing collection.</summary>
        public bool? ChangeStreamPreAndPostImagesEnabled { get; private set; }

        /// <summary>WiredTiger block compressor. Applied only at creation; an immutable option that is never reconciled.</summary>
        public MongoBlockCompressor? BlockCompressor { get; private set; }

        /// <summary>
        /// Marks the collection as capped. The capped flag is applied only when the collection is created
        /// (MongoDB cannot convert an existing collection to capped quickly), but <paramref name="maxSize"/>
        /// and <paramref name="maxDocuments"/> can also be changed later on an already-capped collection.
        /// </summary>
        /// <param name="maxSize">Maximum size in bytes. Required and must be positive.</param>
        /// <param name="maxDocuments">Optional maximum number of documents.</param>
        public MongoCollectionConfigurationBuilder Capped(long maxSize, long? maxDocuments = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);
            if (maxDocuments is <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDocuments), "Capped max documents must be positive when specified.");

            IsCapped = true;
            MaxSize = maxSize;
            MaxDocuments = maxDocuments;

            return this;
        }

        /// <summary>
        /// Changes the size/limits of a capped collection without (re)declaring the capped flag. Use this when the
        /// collection is already capped and you only want to resize it.
        /// </summary>
        public MongoCollectionConfigurationBuilder CappedSize(long maxSize, long? maxDocuments = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);
            if (maxDocuments is <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDocuments), "Capped max documents must be positive when specified.");

            MaxSize = maxSize;
            MaxDocuments = maxDocuments;

            return this;
        }

        /// <summary>Configures document validation.</summary>
        /// <param name="validator">The validation expression (e.g. <c>new BsonDocument("$jsonSchema", ...)</c>).</param>
        /// <param name="level">Optional validation strictness.</param>
        /// <param name="action">Optional action on validation failure.</param>
        public MongoCollectionConfigurationBuilder Validation(BsonDocument validator, DocumentValidationLevel? level = null, DocumentValidationAction? action = null)
        {
            Validator = validator ?? throw new ArgumentNullException(nameof(validator));
            if (level.HasValue)
                ValidationLevel = level;
            if (action.HasValue)
                ValidationAction = action;

            return this;
        }

        /// <summary>Enables or disables recording of change-stream pre/post images.</summary>
        public MongoCollectionConfigurationBuilder ChangeStreamPreAndPostImages(bool enabled = true)
        {
            ChangeStreamPreAndPostImagesEnabled = enabled;

            return this;
        }

        /// <summary>
        /// Sets the WiredTiger block compressor used when the collection is created. This is an immutable,
        /// create-only storage-engine option and is never reconciled onto an existing collection.
        /// <see cref="MongoBlockCompressor.Default"/> clears any declared compressor and leaves the server default.
        /// </summary>
        public MongoCollectionConfigurationBuilder WiredTigerBlockCompressor(MongoBlockCompressor compressor)
        {
            BlockCompressor = compressor == MongoBlockCompressor.Default ? null : compressor;

            return this;
        }

        /// <summary>True when at least one parameter that can be reconciled onto an existing collection has been declared.</summary>
        internal bool HasUpdatableParameters =>
            Validator != null
            || ValidationLevel.HasValue
            || ValidationAction.HasValue
            || MaxSize.HasValue
            || MaxDocuments.HasValue
            || ChangeStreamPreAndPostImagesEnabled.HasValue;
    }
}
