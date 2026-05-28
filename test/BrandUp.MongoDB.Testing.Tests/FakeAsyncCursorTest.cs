using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Driver;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeAsyncCursorTest
    {
        [Fact]
        public void ToList()
        {
            var cursor = new FakeAsyncCursor<string>(new List<string> { "test1", "test2" });

            var items = cursor.ToList(TestContext.Current.CancellationToken);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public void MoveNext_YieldsSingleBatchThenStops()
        {
            var cursor = new FakeAsyncCursor<int>(1, 2, 3);

            Assert.True(cursor.MoveNext(TestContext.Current.CancellationToken));
            Assert.Equal([1, 2, 3], cursor.Current.ToArray());
            Assert.False(cursor.MoveNext(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Current_BeforeMoveNext_Throws()
        {
            var cursor = new FakeAsyncCursor<int>(1);

            Assert.Throws<InvalidOperationException>(() => cursor.Current.ToArray());
        }

        [Fact]
        public void Current_AfterExhausted_Throws()
        {
            var cursor = new FakeAsyncCursor<int>(1);

            cursor.MoveNext(TestContext.Current.CancellationToken);
            cursor.MoveNext(TestContext.Current.CancellationToken);

            Assert.Throws<InvalidOperationException>(() => cursor.Current.ToArray());
        }

        [Fact]
        public void MoveNext_AfterDispose_Throws()
        {
            var cursor = new FakeAsyncCursor<int>(1);
            cursor.Dispose();

            Assert.Throws<ObjectDisposedException>(() => cursor.MoveNext(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void MoveNext_Cancelled_Throws()
        {
            var cursor = new FakeAsyncCursor<int>(1);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => cursor.MoveNext(cts.Token));
        }

        [Fact]
        public void Empty_MoveNext_ReturnsTrueOnceWithEmptyBatch()
        {
            var cursor = new FakeAsyncCursor<int>();

            Assert.True(cursor.MoveNext(TestContext.Current.CancellationToken));
            Assert.Empty(cursor.Current);
            Assert.False(cursor.MoveNext(TestContext.Current.CancellationToken));
        }
    }
}
