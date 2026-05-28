using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace BrandUp.MongoDB.Testing
{
    /// <summary>
    /// In-memory <see cref="IAsyncCursor{TDocument}"/> that yields all of its items in a single batch.
    /// Forward-only and single-pass, mirroring the real driver's cursor contract.
    /// </summary>
    public class FakeAsyncCursor<T> : IAsyncCursor<T>
    {
        readonly List<T> items;
        bool moved;
        bool exhausted;
        bool disposed;

        public FakeAsyncCursor(params T[] items)
        {
            ArgumentNullException.ThrowIfNull(items);

            this.items = new List<T>(items);
        }

        public FakeAsyncCursor(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            this.items = new List<T>(items);
        }

        /// <summary>
        /// The current batch (all items). Throws if accessed before the first <see cref="MoveNext"/>
        /// or after the cursor has been exhausted.
        /// </summary>
        public IEnumerable<T> Current
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);

                if (!moved || exhausted)
                    throw new InvalidOperationException("Enumeration has either not started or has already finished. Call MoveNext before accessing Current.");

                return items;
            }
        }

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            if (moved)
            {
                exhausted = true;
                return false;
            }

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

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
