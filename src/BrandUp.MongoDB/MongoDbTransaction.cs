using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB
{
    /// <summary>
    /// A transaction handle returned by <see cref="MongoDbSession.BeginAsync"/>. If <see cref="CommitAsync"/>
    /// is not called before disposal the transaction is aborted. Supports <c>await using</c>.
    /// </summary>
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

        /// <summary>Commits the outer transaction. A no-op for nested handles.</summary>
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

    /// <summary>
    /// A unit-of-work that commits explicitly via <see cref="CommitAsync"/> or aborts on disposal.
    /// Implementations support both synchronous (<see cref="IDisposable"/>) and asynchronous
    /// (<see cref="IAsyncDisposable"/>) abort paths.
    /// </summary>
    public interface ITransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>Commits the transaction.</summary>
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
