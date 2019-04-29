using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeMongoCollectionTest
    {
        [Fact]
        public async Task CountAsync()
        {
            var client = new FakeMongoClient(MongoUrl.Create("mongodb://localhost:27017"));
            var db = client.GetDatabase("test");
            var collection = db.GetCollection<Document>("test");

            var count = await collection.CountDocumentsAsync(it => it.Name != "test");

            Assert.Equal(0, count);
        }
    }

    public class Document
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
