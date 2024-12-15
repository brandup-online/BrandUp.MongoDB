using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class BsonDocumentTest
    {
        [Fact]
        public void ToBsonDocument()
        {
            var doc = new Document();

            var bsonDocument = doc.ToBsonDocument();

            Assert.NotNull(bsonDocument);
        }

        [Fact]
        public void Merge()
        {
            var doc1 = new Document { Title = "test1" };
            var doc2 = new Document { Title = "test2" };

            var bsonDocument1 = doc1.ToBsonDocument();
            var bsonDocument2 = doc2.ToBsonDocument();

            var bsonResult = bsonDocument1.Merge(bsonDocument2, true);

            Assert.NotNull(bsonResult);
            Assert.Equal("test2", bsonResult["Title"]);
        }

        public class Document
        {
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
            public string Title { get; set; }
        }
    }
}
