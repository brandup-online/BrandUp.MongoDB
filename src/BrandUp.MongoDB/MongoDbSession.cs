using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// A scoped wrapper around <see cref="IClientSessionHandle"/> that exposes a
    /// transaction-factory API. Registered as scoped by <c>AddMongoDb</c>; one session
    /// per DI scope (typically per HTTP request).
    /// </summary>
    public class MongoDbSession : ITransactionFactory, IDisposable, IAsyncDisposable
    {
        readonly IMongoClient client;
        readonly IClientSessionHandle clientSession;
        MongoDbTransaction? transaction;

        /// <summary>The underlying client session handle. Pass this to driver APIs that accept an <see cref="IClientSessionHandle"/>.</summary>
        public IClientSessionHandle Current => clientSession;

        public MongoDbSession(IMongoDbClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            client = clientFactory.ResolveClient();

            clientSession = client.StartSession(new ClientSessionOptions
            {
                CausalConsistency = true,
                Snapshot = false,
                DefaultTransactionOptions = new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority)
            });
        }

        /// <summary>
        /// Starts a transaction on the underlying session, or returns a no-op child handle
        /// when called inside an already-active transaction.
        /// </summary>
        public Task<ITransaction> BeginAsync(CancellationToken cancellationToken = default)
        {
            if (clientSession.IsInTransaction)
            {
                if (transaction == null)
                    return Task.FromResult<ITransaction>(new MongoDbTransaction(this));

                return Task.FromResult<ITransaction>(new MongoDbTransaction(transaction));
            }

            clientSession.StartTransaction();
            transaction = new MongoDbTransaction(this);
            return Task.FromResult<ITransaction>(transaction);
        }

        public void Dispose()
        {
            transaction?.Dispose();
            transaction = null;

            clientSession.Dispose();

            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
                transaction = null;
            }

            clientSession.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Begins a new <see cref="ITransaction"/> on the ambient session.</summary>
    public interface ITransactionFactory
    {
        /// <summary>Starts a transaction. Use <c>await using</c> to ensure the abort path runs asynchronously on failure.</summary>
        Task<ITransaction> BeginAsync(CancellationToken cancellationToken = default);
    }
}
