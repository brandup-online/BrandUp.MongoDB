using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Testing
{
    public class FakeAsyncCursor<T> : IAsyncCursor<T>
    {
        private readonly List<T> items = new List<T>();
        private int index = -1;

        public FakeAsyncCursor(params T[] items)
        {
            this.items.AddRange(items);
        }
        public FakeAsyncCursor(IEnumerable<T> items)
        {
            this.items.AddRange(items);
        }

        public IEnumerable<T> Current => new T[] { items[index] };
        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            if (index == items.Count - 1)
                return false;

            index++;
            return true;
        }
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MoveNext(cancellationToken));
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}