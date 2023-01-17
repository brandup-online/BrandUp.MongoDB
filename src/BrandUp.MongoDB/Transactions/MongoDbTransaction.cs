using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Transactions
{
    public class MongoDbTransaction : ITransaction
    {
        readonly MongoDbTransactionFactory appDocumentSession;
        readonly bool isChild = false;
        private bool disposedValue;

        internal MongoDbTransaction(MongoDbTransactionFactory appDocumentSession, DateTime beginDate)
        {
            this.appDocumentSession = appDocumentSession ?? throw new ArgumentNullException(nameof(appDocumentSession));
            BeginDate = beginDate;
        }
        internal MongoDbTransaction(MongoDbTransaction ownerTransaction)
        {
            if (ownerTransaction == null)
                throw new ArgumentNullException(nameof(ownerTransaction));

            appDocumentSession = ownerTransaction.appDocumentSession;
            BeginDate = ownerTransaction.BeginDate;
            isChild = true;
        }

        #region ITransaction members

        public DateTime BeginDate { get; private set; }
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (!isChild)
            {
                await appDocumentSession.Current.CommitTransactionAsync(cancellationToken);
            }
        }

        #endregion

        #region IDisposable members

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Abort();

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
        }

        #endregion

        private void Abort()
        {
            if (!isChild && appDocumentSession.Current.IsInTransaction)
                appDocumentSession.Current.AbortTransaction();
        }
    }

    public interface ITransaction : IDisposable
    {
        DateTime BeginDate { get; }
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}