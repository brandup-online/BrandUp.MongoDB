using System;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FilterDefinitionTest
    {
        [Fact]
        public void Render_Eq()
        {
            var doc = new Document();

            var filterDefonition = Builders<Document>.Filter
                .Eq(it => it.Title, "test");

            var serializer = BsonSerializer.LookupSerializer<Document>();
            var bsonDocument = filterDefonition.Render(new RenderArgs<Document>(serializer, BsonSerializer.SerializerRegistry));

            Assert.NotNull(bsonDocument);
            Assert.True(bsonDocument.Contains("Title"));
        }

        [Fact]
        public void Render_Text()
        {
            var doc = new Document();

            var filterDefonition = Builders<Document>.Filter
                .Text("test");

            var serializer = BsonSerializer.LookupSerializer<Document>();
            var bsonDocument = filterDefonition.Render(new RenderArgs<Document>(serializer, BsonSerializer.SerializerRegistry));

            Assert.NotNull(bsonDocument);
            Assert.True(bsonDocument.Contains("$text"));
        }

        public class Document
        {
            public Guid Id { get; set; }
            public string Title { get; set; }

            public Document Doc { get; set; }
        }
    }
}
