using System.Net;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace BrandUp.MongoDB.Testing.Internals
{
    /// <summary>
    /// Creates real driver exception and result types (WriteError, BulkWriteError, BulkWriteUpsert)
    /// whose constructors are internal, so that consumer code catching MongoWriteException /
    /// MongoBulkWriteException&lt;T&gt; with ServerErrorCategory.DuplicateKey behaves the same
    /// against the fake as against a real server.
    /// </summary>
    internal static class DriverExceptionFactory
    {
        public const int DuplicateKeyCode = 11000;

        static readonly ConnectionId connectionId = new(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)));

        static readonly ConstructorInfo writeErrorCtor = GetInternalCtor(typeof(WriteError),
            typeof(ServerErrorCategory), typeof(int), typeof(string), typeof(BsonDocument));
        static readonly ConstructorInfo bulkWriteErrorCtor = GetInternalCtor(typeof(BulkWriteError),
            typeof(int), typeof(ServerErrorCategory), typeof(int), typeof(string), typeof(BsonDocument));
        static readonly ConstructorInfo bulkWriteUpsertCtor = GetInternalCtor(typeof(BulkWriteUpsert),
            typeof(int), typeof(BsonValue));

        static ConstructorInfo GetInternalCtor(Type type, params Type[] parameters)
        {
            return type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, parameters)
                ?? throw new MissingMethodException(type.FullName, ".ctor");
        }

        public static MongoWriteException CreateDuplicateKeyException(CollectionNamespace collectionNamespace, string indexName, BsonValue keyValue)
        {
            var message = $"E11000 duplicate key error collection: {collectionNamespace} index: {indexName} dup key: {keyValue}";
            return CreateWriteException(ServerErrorCategory.DuplicateKey, DuplicateKeyCode, message);
        }

        public static MongoWriteException CreateImmutableIdException()
        {
            // Server error code 66 (ImmutableField).
            return CreateWriteException(ServerErrorCategory.Uncategorized, 66, "After applying the update, the (immutable) field '_id' was found to have been altered.");
        }

        public static MongoWriteException CreateUpdateErrorException(string message)
        {
            // Server error code 28 (PathNotViable) is the closest match for update-path failures.
            return CreateWriteException(ServerErrorCategory.Uncategorized, 28, message);
        }

        /// <summary>
        /// findAndModify surfaces write failures as command errors, not write errors —
        /// consumers catch MongoCommandException (with Code 11000 for duplicates) there.
        /// </summary>
        public static MongoCommandException CreateCommandException(int code, string message)
        {
            var result = new BsonDocument { { "ok", 0 }, { "code", code }, { "errmsg", message } };
            return new MongoCommandException(connectionId, message, new BsonDocument("findAndModify", 1), result);
        }

        static MongoWriteException CreateWriteException(ServerErrorCategory category, int code, string message)
        {
            var writeError = (WriteError)writeErrorCtor.Invoke([category, code, message, new BsonDocument()]);

            return new MongoWriteException(connectionId, writeError, null, null);
        }

        public static BulkWriteError CreateBulkError(int index, WriteError source)
        {
            return (BulkWriteError)bulkWriteErrorCtor.Invoke([index, source.Category, source.Code, source.Message, new BsonDocument()]);
        }

        public static BulkWriteUpsert CreateBulkWriteUpsert(int index, BsonValue id)
        {
            return (BulkWriteUpsert)bulkWriteUpsertCtor.Invoke([index, id]);
        }

        public static MongoBulkWriteException<TDocument> CreateBulkWriteException<TDocument>(
            BulkWriteResult<TDocument> result,
            IEnumerable<BulkWriteError> writeErrors,
            IEnumerable<WriteModel<TDocument>> unprocessedRequests)
        {
            return new MongoBulkWriteException<TDocument>(connectionId, result, writeErrors, null, unprocessedRequests);
        }
    }
}
