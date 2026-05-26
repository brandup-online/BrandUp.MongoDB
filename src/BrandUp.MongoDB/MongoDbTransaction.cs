using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB
{
    public class MongoDbTransaction : ITransaction
    {
        readonly MongoDbSession session;
        readonly bool isChild;
        bool isDisposed;

        internal MongoDbTransaction(MongoDbSession session)
        {
            this.session = session;
        }

        internal MongoDbTransaction(MongoDbTransaction ownerTransaction)
        {
            session = ownerTransaction.session;
            isChild = true;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return isChild
                ? Task.CompletedTask
                : session.Current.CommitTransactionAsync(cancellationToken);
        }

        Task AbortAsync(CancellationToken cancellationToken = default)
        {
            return isChild || !session.Current.IsInTransaction
                ? Task.CompletedTask
                : session.Current.AbortTransactionAsync(cancellationToken);
        }

        void AbortSync()
        {
            if (!isChild && session.Current.IsInTransaction)
                session.Current.AbortTransaction();
        }

        #region IDisposable / IAsyncDisposable members

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
                AbortSync();

            isDisposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (isDisposed)
                return;

            await AbortAsync().ConfigureAwait(false);
            isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public interface ITransaction : IDisposable, IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
