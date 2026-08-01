using MongoDB.Bson;

namespace BrandUp.MongoDB.Testing.Internals
{
    /// <summary>
    /// Applies a rendered update document to a BSON document in place. Supports the operators
    /// produced by Builders&lt;T&gt;.Update: $set, $unset, $inc, $mul, $min, $max, $rename,
    /// $currentDate, $setOnInsert and the array operators $addToSet, $push, $pull.
    /// </summary>
    internal static class BsonUpdateEngine
    {
        /// <param name="update">Rendered update document ({$set: {...}, $inc: {...}}).</param>
        /// <param name="document">Document to mutate.</param>
        /// <param name="insertMode">True when applying to a document being created by an upsert ($setOnInsert applies).</param>
        public static void Apply(BsonDocument update, BsonDocument document, bool insertMode)
        {
            foreach (var operation in update.Elements)
            {
                switch (operation.Name)
                {
                    case "$set":
                        foreach (var field in operation.Value.AsBsonDocument)
                            SetValue(document, field.Name, field.Value);
                        break;
                    case "$setOnInsert":
                        if (insertMode)
                        {
                            foreach (var field in operation.Value.AsBsonDocument)
                                SetValue(document, field.Name, field.Value);
                        }
                        break;
                    case "$unset":
                        foreach (var field in operation.Value.AsBsonDocument)
                            UnsetValue(document, field.Name);
                        break;
                    case "$inc":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var current = GetValue(document, field.Name);
                            if (current == null || current.IsBsonNull)
                                SetValue(document, field.Name, field.Value);
                            else
                                SetValue(document, field.Name, AddNumeric(current, field.Value));
                        }
                        break;
                    case "$mul":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var current = GetValue(document, field.Name);
                            if (current == null || current.IsBsonNull)
                                SetValue(document, field.Name, MultiplyNumeric(field.Value, new BsonInt32(0)));
                            else
                                SetValue(document, field.Name, MultiplyNumeric(current, field.Value));
                        }
                        break;
                    // Unlike query comparisons, $min/$max use the total BSON canonical order across types.
                    case "$min":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var current = GetValue(document, field.Name);
                            if (current == null || field.Value.CompareTo(current) < 0)
                                SetValue(document, field.Name, field.Value);
                        }
                        break;
                    case "$max":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var current = GetValue(document, field.Name);
                            if (current == null || field.Value.CompareTo(current) > 0)
                                SetValue(document, field.Name, field.Value);
                        }
                        break;
                    case "$rename":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var current = GetValue(document, field.Name);
                            if (current != null)
                            {
                                UnsetValue(document, field.Name);
                                SetValue(document, field.Value.AsString, current);
                            }
                        }
                        break;
                    case "$currentDate":
                        foreach (var field in operation.Value.AsBsonDocument)
                            SetValue(document, field.Name, new BsonDateTime(DateTime.UtcNow));
                        break;
                    case "$addToSet":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var array = GetOrCreateArray(document, field.Name);
                            foreach (var item in ExpandEach(field.Value))
                            {
                                if (!array.Any(existing => BsonValueHelper.ValuesEqual(existing, item)))
                                    array.Add(item);
                            }
                        }
                        break;
                    case "$push":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var array = GetOrCreateArray(document, field.Name);
                            foreach (var item in ExpandEach(field.Value))
                                array.Add(item);
                        }
                        break;
                    case "$pull":
                        foreach (var field in operation.Value.AsBsonDocument)
                        {
                            var current = GetValue(document, field.Name);
                            if (current is not BsonArray array)
                                continue;

                            var remaining = new BsonArray(array.Where(item => !PullMatches(item, field.Value)));
                            SetValue(document, field.Name, remaining);
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Update operator \"{operation.Name}\" is not supported by the in-memory fake.");
                }
            }
        }

        /// <summary>
        /// Builds the seed document for an upsert insert from the equality conditions of a rendered filter,
        /// the way the MongoDB server does.
        /// </summary>
        public static BsonDocument BuildUpsertSeed(BsonDocument filter)
        {
            var seed = new BsonDocument();
            CollectEqualities(filter, seed);
            return seed;
        }

        static void CollectEqualities(BsonDocument filter, BsonDocument seed)
        {
            foreach (var element in filter.Elements)
            {
                if (element.Name == "$and")
                {
                    foreach (var clause in element.Value.AsBsonArray)
                        CollectEqualities(clause.AsBsonDocument, seed);
                    continue;
                }

                if (element.Name.StartsWith('$'))
                    continue;

                if (element.Value is BsonDocument conditionDoc && conditionDoc.ElementCount > 0 && conditionDoc.Elements.All(it => it.Name.StartsWith('$')))
                {
                    if (conditionDoc.TryGetValue("$eq", out var eqValue))
                        SetValue(seed, element.Name, eqValue);
                    continue;
                }

                if (element.Value is BsonRegularExpression)
                    continue;

                SetValue(seed, element.Name, element.Value);
            }
        }

        static IEnumerable<BsonValue> ExpandEach(BsonValue value)
        {
            if (value is BsonDocument doc && doc.Contains("$each"))
            {
                foreach (var modifier in doc.Elements)
                {
                    if (modifier.Name != "$each")
                        throw new NotSupportedException($"Array update modifier \"{modifier.Name}\" is not supported by the in-memory fake.");
                }

                return doc["$each"].AsBsonArray;
            }

            return [value];
        }

        static bool PullMatches(BsonValue item, BsonValue condition)
        {
            if (condition is BsonDocument conditionDoc && conditionDoc.ElementCount > 0)
            {
                if (conditionDoc.Elements.All(it => it.Name.StartsWith('$')))
                    return item is BsonDocument == false && MatchesOperatorsOnScalar(item, conditionDoc);

                return item is BsonDocument itemDoc && BsonFilterMatcher.Matches(conditionDoc, itemDoc);
            }

            return BsonValueHelper.ValuesEqual(item, condition);
        }

        static bool MatchesOperatorsOnScalar(BsonValue item, BsonDocument operators)
        {
            var wrapper = new BsonDocument("v", item);
            var filter = new BsonDocument("v", operators);
            return BsonFilterMatcher.Matches(filter, wrapper);
        }

        static BsonArray GetOrCreateArray(BsonDocument document, string path)
        {
            var current = GetValue(document, path);
            if (current is BsonArray array)
                return array;
            // A missing field is created; an existing non-array value (including null) is a server error.
            if (current != null)
                throw new InvalidOperationException($"The field '{path}' must be an array but is of type {current.BsonType.ToString().ToLowerInvariant()}.");

            var created = new BsonArray();
            SetValue(document, path, created);
            return created;
        }

        static BsonValue AddNumeric(BsonValue left, BsonValue right)
        {
            if (!BsonValueHelper.IsNumeric(left) || !BsonValueHelper.IsNumeric(right))
                throw new InvalidOperationException("Cannot apply $inc to a non-numeric value.");

            if (left.BsonType == BsonType.Decimal128 || right.BsonType == BsonType.Decimal128)
                return new BsonDecimal128(left.AsDecimal + right.AsDecimal);
            if (left.BsonType == BsonType.Double || right.BsonType == BsonType.Double)
                return new BsonDouble(left.ToDouble() + right.ToDouble());
            if (left.BsonType == BsonType.Int64 || right.BsonType == BsonType.Int64)
                return new BsonInt64(left.ToInt64() + right.ToInt64());
            return new BsonInt32(left.ToInt32() + right.ToInt32());
        }

        static BsonValue MultiplyNumeric(BsonValue left, BsonValue right)
        {
            if (!BsonValueHelper.IsNumeric(left) || !BsonValueHelper.IsNumeric(right))
                throw new InvalidOperationException("Cannot apply $mul to a non-numeric value.");

            if (left.BsonType == BsonType.Decimal128 || right.BsonType == BsonType.Decimal128)
                return new BsonDecimal128(left.AsDecimal * right.AsDecimal);
            if (left.BsonType == BsonType.Double || right.BsonType == BsonType.Double)
                return new BsonDouble(left.ToDouble() * right.ToDouble());
            if (left.BsonType == BsonType.Int64 || right.BsonType == BsonType.Int64)
                return new BsonInt64(left.ToInt64() * right.ToInt64());
            return new BsonInt32(left.ToInt32() * right.ToInt32());
        }

        static BsonValue? GetValue(BsonDocument document, string path)
        {
            BsonValue current = document;
            foreach (var segment in path.Split('.'))
            {
                if (current is BsonDocument doc)
                {
                    if (!doc.TryGetValue(segment, out var next))
                        return null;
                    current = next;
                }
                else if (current is BsonArray array && int.TryParse(segment, out var index) && index >= 0 && index < array.Count)
                {
                    current = array[index];
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        static void SetValue(BsonDocument document, string path, BsonValue value)
        {
            var segments = path.Split('.');
            BsonValue current = document;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];

                if (current is BsonDocument doc)
                {
                    if (!doc.TryGetValue(segment, out var next))
                    {
                        next = new BsonDocument();
                        doc[segment] = next;
                    }
                    else if (next is not (BsonDocument or BsonArray))
                    {
                        // The server refuses to implicitly convert an existing scalar into a document.
                        throw new InvalidOperationException($"Cannot create field \"{segments[i + 1]}\" in element {{{segment}: {next}}}.");
                    }
                    current = next;
                }
                else if (current is BsonArray array && int.TryParse(segment, out var index))
                {
                    var isNewSlot = index >= array.Count;
                    while (array.Count <= index)
                        array.Add(BsonNull.Value);
                    if (array[index] is not (BsonDocument or BsonArray))
                    {
                        if (!isNewSlot)
                            throw new InvalidOperationException($"Cannot create field \"{segments[i + 1]}\" in element {{{segment}: {array[index]}}}.");
                        array[index] = new BsonDocument();
                    }
                    current = array[index];
                }
                else
                {
                    throw new InvalidOperationException($"Cannot create path \"{path}\".");
                }
            }

            var last = segments[^1];
            if (current is BsonDocument targetDoc)
            {
                targetDoc[last] = value;
            }
            else if (current is BsonArray targetArray && int.TryParse(last, out var lastIndex))
            {
                while (targetArray.Count <= lastIndex)
                    targetArray.Add(BsonNull.Value);
                targetArray[lastIndex] = value;
            }
            else
            {
                throw new InvalidOperationException($"Cannot set value at path \"{path}\".");
            }
        }

        static void UnsetValue(BsonDocument document, string path)
        {
            var segments = path.Split('.');
            BsonValue current = document;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (current is BsonDocument doc && doc.TryGetValue(segments[i], out var next))
                    current = next;
                else if (current is BsonArray array && int.TryParse(segments[i], out var index) && index >= 0 && index < array.Count)
                    current = array[index];
                else
                    return;
            }

            var last = segments[^1];
            if (current is BsonDocument targetDoc)
                targetDoc.Remove(last);
            else if (current is BsonArray targetArray && int.TryParse(last, out var lastIndex) && lastIndex >= 0 && lastIndex < targetArray.Count)
                targetArray[lastIndex] = BsonNull.Value; // Mongo replaces unset array slots with null.
        }
    }

    /// <summary>Orders documents by a rendered sort specification ({field: 1|-1, ...}).</summary>
    internal sealed class BsonSortComparer(BsonDocument sort) : IComparer<BsonDocument>
    {
        public int Compare(BsonDocument? x, BsonDocument? y)
        {
            foreach (var element in sort.Elements)
            {
                var direction = element.Value.ToInt32();
                var left = BsonValueHelper.ResolveSingle(x!, element.Name) ?? BsonNull.Value;
                var right = BsonValueHelper.ResolveSingle(y!, element.Name) ?? BsonNull.Value;

                var comparison = left.CompareTo(right);
                if (comparison != 0)
                    return direction >= 0 ? comparison : -comparison;
            }

            return 0;
        }
    }
}
