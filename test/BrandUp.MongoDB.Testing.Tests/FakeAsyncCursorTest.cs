using System.Collections.Generic;
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
    }
}