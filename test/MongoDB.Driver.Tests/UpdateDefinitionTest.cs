using MongoDB.Bson.Serialization;
using System;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class UpdateDefinitionTest
    {
        [Fact]
        public void Render()
        {
            var doc = new Document();

            var updateDefonition = Builders<Document>.Update
                .Set(it => it.Title, "test")
                .Set(it => it.Doc.Title, "test2");

            var serializer = BsonSerializer.LookupSerializer<Document>();
            var bsonDocument = updateDefonition.Render(serializer, BsonSerializer.SerializerRegistry);

            Assert.NotNull(bsonDocument);
            Assert.True(bsonDocument.AsBsonDocument.Contains("$set"));
        }

        public class Document
        {
            public Guid Id { get; set; }
            public string Title { get; set; }

            public Document Doc { get; set; }
        }
    }
}
