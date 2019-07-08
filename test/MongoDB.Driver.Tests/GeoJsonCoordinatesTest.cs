using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class GeoJsonCoordinatesTest
    {
        [Fact]
        public void Render_Eq()
        {
            var point = GeoJson.Point(new Location());

            var bsonDocument = point.ToBsonDocument();

            Assert.NotNull(bsonDocument);
        }

        public class Location : GeoJsonCoordinates, IEnumerable<double>
        {
            readonly List<double> coordinates = new List<double> { 0, 0 };

            public override ReadOnlyCollection<double> Values => new ReadOnlyCollection<double>(coordinates);

            [BsonIgnore]
            public double Longitude { get => coordinates[0]; set => coordinates[0] = value; }
            [BsonIgnore]
            public double Latitude { get => coordinates[1]; set => coordinates[1] = value; }

            #region IEnumerable members

            public IEnumerator<double> GetEnumerator()
            {
                return Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }
    }
}