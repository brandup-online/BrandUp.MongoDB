using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB
{
    public class MongoDbTransaction : ITransaction
    {
        readonly MongoDbSession appDocumentSession;
        readonly bool isChild = false;
        bool isDisposed;

        internal MongoDbTransaction(MongoDbSession appDocumentSession)
        {
            this.appDocumentSession = appDocumentSession ?? throw new ArgumentNullException(nameof(appDocumentSession));
        }

        internal MongoDbTransaction(MongoDbTransaction ownerTransaction)
        {
            ArgumentNullException.ThrowIfNull(ownerTransaction);

            appDocumentSession = ownerTransaction.appDocumentSession;
            isChild = true;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (!isChild)
                await appDocumentSession.Current.CommitTransactionAsync(cancellationToken);
        }

        void Abort()
        {
            if (!isChild && appDocumentSession.Current.IsInTransaction)
                appDocumentSession.Current.AbortTransaction();
        }

        #region IDisposable members

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                    Abort();

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

    public interface ITransaction : IDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
