using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class IBsonSerializerTest
    {
        [Fact]
        public void Serialize()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "test" };

            var serializedDocument = new BsonDocument();
            using (var bsonReader = new BsonDocumentWriter(serializedDocument))
            {
                var serializer = BsonSerializer.LookupSerializer<Document>();
                var bsonSerializationContext = BsonSerializationContext.CreateRoot(bsonReader);
                serializer.Serialize(bsonSerializationContext, default, doc);
            }

            Assert.Equal(doc.Id, serializedDocument["_id"].AsGuid);
            Assert.Equal(doc.Title, serializedDocument["Title"]);
        }

        [Fact]
        public void Deserialize()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "test" };
            Document deserializedDoc;

            using (var bsonReader = new BsonDocumentReader(doc.ToBsonDocument()))
            {
                var serializer = BsonSerializer.LookupSerializer<Document>();
                var bsonDeserializationContext = BsonDeserializationContext.CreateRoot(bsonReader);
                deserializedDoc = serializer.Deserialize(bsonDeserializationContext);
            }

            Assert.Equal(doc.Id, deserializedDoc.Id);
            Assert.Equal(doc.Title, deserializedDoc.Title);
        }

        public class Document
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
        }
    }
}
