using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Testing
{
    public class FakeClientSessionHandle : IClientSessionHandle
    {
        readonly FakeMongoClient client;
        readonly BsonDocument clusterTime;
        readonly BsonTimestamp operationTime;
        readonly ClientSessionOptions options;
        private bool isInTransaction = false;

        public IMongoClient Client => client;
        public BsonDocument ClusterTime => clusterTime;
        public bool IsImplicit => false;
        public bool IsInTransaction => isInTransaction;
        public BsonTimestamp OperationTime => operationTime;
        public ClientSessionOptions Options => options;
        public IServerSession ServerSession => throw new NotImplementedException();
        public ICoreSessionHandle WrappedCoreSession => throw new NotImplementedException();

        public FakeClientSessionHandle(FakeMongoClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            clusterTime = new BsonDocument();
            operationTime = new BsonTimestamp(10);
            options = new ClientSessionOptions();
        }

        public void AbortTransaction(CancellationToken cancellationToken = default)
        {
            if (!isInTransaction)
                throw new InvalidOperationException();

            isInTransaction = false;
        }
        public Task AbortTransactionAsync(CancellationToken cancellationToken = default)
        {
            AbortTransaction(cancellationToken);

            return Task.CompletedTask;
        }

        public void AdvanceClusterTime(BsonDocument newClusterTime) { }
        public void AdvanceOperationTime(BsonTimestamp newOperationTime) { }

        public void CommitTransaction(CancellationToken cancellationToken = default)
        {
            if (!isInTransaction)
                throw new InvalidOperationException();

            isInTransaction = false;
        }
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            CommitTransaction(cancellationToken);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public IClientSessionHandle Fork()
        {
            return new FakeClientSessionHandle(client);
        }

        public void StartTransaction(TransactionOptions transactionOptions = null)
        {
            if (isInTransaction)
                throw new InvalidOperationException();

            isInTransaction = true;
        }

        public TResult WithTransaction<TResult>(Func<IClientSessionHandle, CancellationToken, TResult> callback, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default)
        {
            if (!isInTransaction)
                throw new InvalidOperationException();

            return callback(this, cancellationToken);
        }

        public Task<TResult> WithTransactionAsync<TResult>(Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default)
        {
            if (!isInTransaction)
                throw new InvalidOperationException();

            return callbackAsync(this, cancellationToken);
        }
    }
}