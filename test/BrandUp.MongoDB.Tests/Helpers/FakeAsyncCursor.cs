using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Tests
{
    public class FakeAsyncCursor<T> : IAsyncCursor<T>
    {
        private readonly List<T> items = new List<T>();
        private int index = -1;

        public FakeAsyncCursor(params T[] items)
        {
            this.items.AddRange(items);
        }

        public IEnumerable<T> Current => items;

        public void Dispose()
        {
        }

        public bool MoveNext(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (index == items.Count - 1)
                return false;

            index++;
            return true;
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(MoveNext(cancellationToken));
        }
    }
}