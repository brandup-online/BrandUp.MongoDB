using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public class MongoDbSession : ITransactionFactory, IDisposable, IAsyncDisposable
    {
        readonly IMongoClient client;
        readonly IClientSessionHandle clientSession;
        MongoDbTransaction? transaction;

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

        public Task<ITransaction> BeginAsync(CancellationToken cancellationToken = default)
        {
            if (clientSession.IsInTransaction)
            {
                if (transaction == null)
                    throw new InvalidOperationException("Session is in a transaction that was not started through this MongoDbSession.");

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

    public interface ITransactionFactory
    {
        Task<ITransaction> BeginAsync(CancellationToken cancellationToken = default);
    }
}
