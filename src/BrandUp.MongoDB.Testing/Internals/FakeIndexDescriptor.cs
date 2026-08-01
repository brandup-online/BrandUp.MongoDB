using MongoDB.Bson;

namespace BrandUp.MongoDB.Testing.Internals
{
    /// <summary>Rendered index definition retained by the fake index manager for constraint enforcement.</summary>
    internal sealed class FakeIndexDescriptor
    {
        public required string Name { get; init; }
        public required BsonDocument Keys { get; init; }
        public bool Unique { get; init; }
        public bool Sparse { get; init; }
        public BsonDocument? PartialFilter { get; init; }
        /// <summary>Approximation of a collation with strength Primary/Secondary: string keys compare case-insensitively.</summary>
        public bool CaseInsensitive { get; init; }

        public IEnumerable<string> KeyFields => Keys.Names;

        /// <summary>Whether the document contributes an entry to this index (sparse and partial semantics).</summary>
        public bool Participates(BsonDocument document)
        {
            if (Sparse && !KeyFields.Any(field => BsonValueHelper.ResolveSingle(document, field) != null))
                return false;

            if (PartialFilter != null && !BsonFilterMatcher.Matches(PartialFilter, document))
                return false;

            return true;
        }

        public BsonValue[] ExtractKey(BsonDocument document)
        {
            return [.. KeyFields.Select(field => BsonValueHelper.ResolveSingle(document, field) ?? BsonNull.Value)];
        }

        public bool KeysEqual(BsonValue[] left, BsonValue[] right)
        {
            for (var i = 0; i < left.Length; i++)
            {
                if (CaseInsensitive && left[i].BsonType == BsonType.String && right[i].BsonType == BsonType.String)
                {
                    if (!string.Equals(left[i].AsString, right[i].AsString, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                else if (!BsonValueHelper.ValuesEqual(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
