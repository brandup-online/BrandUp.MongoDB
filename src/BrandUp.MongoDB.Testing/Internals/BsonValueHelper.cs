using MongoDB.Bson;

namespace BrandUp.MongoDB.Testing.Internals
{
    /// <summary>
    /// Value comparison semantics shared by the filter matcher, update engine and index enforcement.
    /// Mirrors MongoDB behaviour: numeric types compare by value regardless of BSON representation,
    /// values of different type classes never satisfy range comparisons.
    /// </summary>
    internal static class BsonValueHelper
    {
        public static bool IsNumeric(BsonValue value)
        {
            return value.BsonType is BsonType.Int32 or BsonType.Int64 or BsonType.Double or BsonType.Decimal128;
        }

        /// <summary>Equality with MongoDB semantics: cross-numeric by value, otherwise strict BSON equality.</summary>
        public static bool ValuesEqual(BsonValue left, BsonValue right)
        {
            if (IsNumeric(left) && IsNumeric(right))
                return left.CompareTo(right) == 0;

            return left.Equals(right);
        }

        /// <summary>
        /// Range comparison. Returns false when the values belong to different BSON type classes
        /// (Mongo never matches e.g. a string against {$gt: 5}).
        /// </summary>
        public static bool TryCompare(BsonValue left, BsonValue right, out int result)
        {
            result = 0;

            if (!SameTypeClass(left, right))
                return false;

            result = left.CompareTo(right);
            return true;
        }

        public static bool SameTypeClass(BsonValue left, BsonValue right)
        {
            return GetTypeClass(left) == GetTypeClass(right);
        }

        static int GetTypeClass(BsonValue value)
        {
            return value.BsonType switch
            {
                BsonType.Int32 or BsonType.Int64 or BsonType.Double or BsonType.Decimal128 => 1,
                BsonType.String or BsonType.Symbol => 2,
                BsonType.Document => 3,
                BsonType.Array => 4,
                BsonType.Binary => 5,
                BsonType.ObjectId => 6,
                BsonType.Boolean => 7,
                BsonType.DateTime => 8,
                BsonType.Timestamp => 9,
                BsonType.RegularExpression => 10,
                BsonType.Null or BsonType.Undefined => 11,
                _ => 0
            };
        }

        /// <summary>Aggregation truthiness: null, undefined, false and numeric zero are falsy.</summary>
        public static bool IsTruthy(BsonValue value)
        {
            return value.BsonType switch
            {
                BsonType.Null or BsonType.Undefined => false,
                BsonType.Boolean => value.AsBoolean,
                BsonType.Int32 => value.AsInt32 != 0,
                BsonType.Int64 => value.AsInt64 != 0,
                BsonType.Double => value.AsDouble != 0,
                BsonType.Decimal128 => value.AsDecimal != 0,
                _ => true
            };
        }

        /// <summary>
        /// Resolves a (possibly dotted) path against a document, fanning out through arrays the way
        /// the MongoDB query engine does. Returns every terminal value; <paramref name="found"/> is
        /// false when the path does not exist in the document at all.
        /// </summary>
        public static List<BsonValue> ResolvePath(BsonDocument document, string path, out bool found)
        {
            var results = new List<BsonValue>();
            Resolve(document, path.Split('.'), 0, results);
            found = results.Count > 0;
            return results;
        }

        static void Resolve(BsonValue current, string[] segments, int index, List<BsonValue> results)
        {
            if (index == segments.Length)
            {
                results.Add(current);
                return;
            }

            var segment = segments[index];

            if (current is BsonDocument document)
            {
                if (document.TryGetValue(segment, out var value))
                    Resolve(value, segments, index + 1, results);
                return;
            }

            if (current is BsonArray array)
            {
                if (int.TryParse(segment, out var arrayIndex) && arrayIndex >= 0 && arrayIndex < array.Count)
                    Resolve(array[arrayIndex], segments, index + 1, results);

                foreach (var element in array)
                {
                    if (element is BsonDocument)
                        Resolve(element, segments, index, results);
                }
            }
        }

        /// <summary>Resolves a path to its single (first) terminal value, or null when missing.</summary>
        public static BsonValue? ResolveSingle(BsonDocument document, string path)
        {
            var values = ResolvePath(document, path, out var found);
            return found ? values[0] : null;
        }
    }
}
