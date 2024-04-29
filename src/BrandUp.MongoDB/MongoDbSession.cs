using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public class MongoDbSession : ITransactionFactory, IDisposable
    {
        readonly IMongoClient client;
        readonly IClientSessionHandle clientSession;
        MongoDbTransaction transaction;

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
            if (!clientSession.IsInTransaction)
            {
                clientSession.StartTransaction();
                transaction = new MongoDbTransaction(this);

                return Task.FromResult<ITransaction>(transaction);
            }

            return Task.FromResult<ITransaction>(new MongoDbTransaction(transaction));
        }

        public void Dispose()
        {
            if (transaction != null)
            {
                transaction.Dispose();
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