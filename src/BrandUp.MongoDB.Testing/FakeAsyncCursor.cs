using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    public class FakeAsyncCursor<T> : IAsyncCursor<T>
    {
        readonly List<T> items;
        bool moved;
        bool disposed;

        public FakeAsyncCursor(params T[] items)
        {
            this.items = new List<T>(items);
        }

        public FakeAsyncCursor(IEnumerable<T> items)
        {
            this.items = new List<T>(items);
        }

        public IEnumerable<T> Current => items;

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            if (moved)
                return false;

            moved = true;
            return true;
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MoveNext(cancellationToken));
        }

        #region IDisposable members

        protected virtual void Dispose(bool disposing)
        {
            disposed = true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Dispose(true);
        }

        #endregion
    }
}
