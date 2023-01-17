using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Transactions
{
    public class MongoDbTransactionFactory : ITransactionFactory, IDisposable
    {
        readonly IClientSessionHandle clientSession;
        private MongoDbTransaction transaction;
        private bool isDisposed;

        public IClientSessionHandle Current => clientSession;

        public MongoDbTransactionFactory(MongoDbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            clientSession = context.Database.Client.StartSession(new ClientSessionOptions
            {
                CausalConsistency = true,
                Snapshot = false,
                DefaultTransactionOptions = new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority)
            });
        }

        public Task<ITransaction> BeginAsync(CancellationToken cancellationToken = default)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(isDisposed));

            if (!clientSession.IsInTransaction)
            {
                clientSession.StartTransaction();
                transaction = new MongoDbTransaction(this, DateTime.UtcNow);

                return Task.FromResult<ITransaction>(transaction);
            }

            return Task.FromResult<ITransaction>(new MongoDbTransaction(transaction));
        }

        #region IDisposable members

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (transaction != null)
                    {
                        transaction.Dispose();
                        transaction = null;
                    }

                    clientSession.Dispose();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public interface ITransactionFactory
    {
        Task<ITransaction> BeginAsync(CancellationToken cancellationToken = default);
    }
}