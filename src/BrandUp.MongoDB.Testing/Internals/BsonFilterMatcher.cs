using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace BrandUp.MongoDB.Testing.Internals
{
    /// <summary>
    /// Evaluates a rendered MongoDB filter document against a BSON document. Covers the operators
    /// used through Builders&lt;T&gt;.Filter: comparison, logical, element ($exists/$type), $regex,
    /// array operators ($in/$nin/$all/$size/$elemMatch) and a subset of $expr.
    /// </summary>
    internal static class BsonFilterMatcher
    {
        public static bool Matches(BsonDocument filter, BsonDocument document)
        {
            foreach (var element in filter.Elements)
            {
                switch (element.Name)
                {
                    case "$and":
                        if (!element.Value.AsBsonArray.All(clause => Matches(clause.AsBsonDocument, document)))
                            return false;
                        break;
                    case "$or":
                        if (!element.Value.AsBsonArray.Any(clause => Matches(clause.AsBsonDocument, document)))
                            return false;
                        break;
                    case "$nor":
                        if (element.Value.AsBsonArray.Any(clause => Matches(clause.AsBsonDocument, document)))
                            return false;
                        break;
                    case "$expr":
                        if (!BsonValueHelper.IsTruthy(BsonAggregateExpression.Evaluate(element.Value, document)))
                            return false;
                        break;
                    case "$comment":
                        break;
                    default:
                        if (element.Name.StartsWith('$'))
                            throw new NotSupportedException($"Filter operator \"{element.Name}\" is not supported by the in-memory fake.");
                        if (!MatchesField(document, element.Name, element.Value))
                            return false;
                        break;
                }
            }

            return true;
        }

        static bool MatchesField(BsonDocument document, string path, BsonValue condition)
        {
            var values = BsonValueHelper.ResolvePath(document, path, out var found);

            if (condition is BsonDocument conditionDoc && IsOperatorDocument(conditionDoc))
                return ApplyOperators(values, found, conditionDoc);

            return EqualityMatches(values, found, condition);
        }

        static bool IsOperatorDocument(BsonDocument document)
        {
            return document.ElementCount > 0 && document.Elements.All(it => it.Name.StartsWith('$'));
        }

        static bool EqualityMatches(List<BsonValue> values, bool found, BsonValue condition)
        {
            // {field: null} also matches documents where the field is missing.
            if (condition.IsBsonNull)
                return !found || values.Any(v => v.IsBsonNull);

            if (condition is BsonRegularExpression regex)
                return RegexMatchesAny(values, regex);

            foreach (var value in values)
            {
                if (BsonValueHelper.ValuesEqual(value, condition))
                    return true;

                if (value is BsonArray array && array.Any(item => BsonValueHelper.ValuesEqual(item, condition)))
                    return true;
            }

            return false;
        }

        static bool ApplyOperators(List<BsonValue> values, bool found, BsonDocument operators)
        {
            // $regex and $options form a single condition.
            string? regexOptions = operators.TryGetValue("$options", out var optionsValue) ? optionsValue.AsString : null;

            foreach (var op in operators.Elements)
            {
                var satisfied = op.Name switch
                {
                    "$eq" => EqualityMatches(values, found, op.Value),
                    "$ne" => !EqualityMatches(values, found, op.Value),
                    "$gt" => CompareAny(values, op.Value, r => r > 0),
                    "$gte" => CompareAny(values, op.Value, r => r >= 0),
                    "$lt" => CompareAny(values, op.Value, r => r < 0),
                    "$lte" => CompareAny(values, op.Value, r => r <= 0),
                    "$in" => InMatches(values, found, op.Value.AsBsonArray),
                    "$nin" => !InMatches(values, found, op.Value.AsBsonArray),
                    "$exists" => found == BsonValueHelper.IsTruthy(op.Value),
                    "$type" => TypeMatches(values, op.Value),
                    "$not" => !ApplyNot(values, found, op.Value),
                    "$regex" => RegexMatchesAny(values, ToRegex(op.Value, regexOptions)),
                    "$options" => true,
                    "$size" => values.Any(v => v is BsonArray arr && arr.Count == op.Value.ToInt32()),
                    // {$all: []} matches nothing on a real server.
                    "$all" => op.Value.AsBsonArray is { Count: > 0 } allValues && allValues.All(required => EqualityMatches(values, found, required)),
                    "$elemMatch" => ElemMatches(values, op.Value.AsBsonDocument),
                    _ => throw new NotSupportedException($"Filter operator \"{op.Name}\" is not supported by the in-memory fake.")
                };

                if (!satisfied)
                    return false;
            }

            return true;
        }

        static bool CompareAny(List<BsonValue> values, BsonValue operand, Func<int, bool> predicate)
        {
            foreach (var value in values)
            {
                if (BsonValueHelper.TryCompare(value, operand, out var result) && predicate(result))
                    return true;

                if (value is BsonArray array)
                {
                    foreach (var item in array)
                    {
                        if (BsonValueHelper.TryCompare(item, operand, out var itemResult) && predicate(itemResult))
                            return true;
                    }
                }
            }

            return false;
        }

        static bool InMatches(List<BsonValue> values, bool found, BsonArray candidates)
        {
            foreach (var candidate in candidates)
            {
                if (EqualityMatches(values, found, candidate))
                    return true;
            }

            return false;
        }

        static bool TypeMatches(List<BsonValue> values, BsonValue typeSpec)
        {
            var allowed = typeSpec is BsonArray typeArray ? typeArray.ToList() : [typeSpec];

            foreach (var value in values)
            {
                foreach (var spec in allowed)
                {
                    if (MatchesTypeAlias(value, spec))
                        return true;
                }
            }

            return false;
        }

        static bool MatchesTypeAlias(BsonValue value, BsonValue spec)
        {
            if (spec.IsNumeric)
                return (int)value.BsonType == spec.ToInt32();

            var alias = spec.AsString;
            if (alias == "number")
                return BsonValueHelper.IsNumeric(value);

            return alias switch
            {
                "double" => value.BsonType == BsonType.Double,
                "string" => value.BsonType == BsonType.String,
                "object" => value.BsonType == BsonType.Document,
                "array" => value.BsonType == BsonType.Array,
                "binData" => value.BsonType == BsonType.Binary,
                "undefined" => value.BsonType == BsonType.Undefined,
                "objectId" => value.BsonType == BsonType.ObjectId,
                "bool" => value.BsonType == BsonType.Boolean,
                "date" => value.BsonType == BsonType.DateTime,
                "null" => value.BsonType == BsonType.Null,
                "regex" => value.BsonType == BsonType.RegularExpression,
                "javascript" => value.BsonType == BsonType.JavaScript,
                "symbol" => value.BsonType == BsonType.Symbol,
                "int" => value.BsonType == BsonType.Int32,
                "timestamp" => value.BsonType == BsonType.Timestamp,
                "long" => value.BsonType == BsonType.Int64,
                "decimal" => value.BsonType == BsonType.Decimal128,
                "minKey" => value.BsonType == BsonType.MinKey,
                "maxKey" => value.BsonType == BsonType.MaxKey,
                _ => throw new NotSupportedException($"Unknown $type alias \"{alias}\".")
            };
        }

        static bool ApplyNot(List<BsonValue> values, bool found, BsonValue operand)
        {
            if (operand is BsonRegularExpression regex)
                return RegexMatchesAny(values, regex);

            return ApplyOperators(values, found, operand.AsBsonDocument);
        }

        static bool RegexMatchesAny(List<BsonValue> values, BsonRegularExpression regex)
        {
            foreach (var value in values)
            {
                if (RegexMatches(value, regex))
                    return true;

                if (value is BsonArray array && array.Any(item => RegexMatches(item, regex)))
                    return true;
            }

            return false;
        }

        static bool ElemMatches(List<BsonValue> values, BsonDocument condition)
        {
            foreach (var value in values)
            {
                if (value is not BsonArray array)
                    continue;

                foreach (var item in array)
                {
                    if (IsOperatorDocument(condition))
                    {
                        if (ApplyOperators([item], true, condition))
                            return true;
                    }
                    else if (item is BsonDocument itemDoc && Matches(condition, itemDoc))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static BsonRegularExpression ToRegex(BsonValue value, string? options)
        {
            if (value is BsonRegularExpression regex)
                return options == null ? regex : new BsonRegularExpression(regex.Pattern, options);

            return new BsonRegularExpression(value.AsString, options ?? "");
        }

        static bool RegexMatches(BsonValue value, BsonRegularExpression regex)
        {
            if (value.BsonType != BsonType.String)
                return false;

            var regexOptions = RegexOptions.None;
            foreach (var option in regex.Options)
            {
                regexOptions |= option switch
                {
                    'i' => RegexOptions.IgnoreCase,
                    'm' => RegexOptions.Multiline,
                    's' => RegexOptions.Singleline,
                    'x' => RegexOptions.IgnorePatternWhitespace,
                    _ => RegexOptions.None
                };
            }

            return Regex.IsMatch(value.AsString, regex.Pattern, regexOptions);
        }
    }

    /// <summary>
    /// Minimal aggregation-expression evaluator backing $expr: field paths, literals,
    /// comparison and logical operators, $in, $ifNull and $cond.
    /// </summary>
    internal static class BsonAggregateExpression
    {
        public static BsonValue Evaluate(BsonValue expression, BsonDocument document)
        {
            if (expression is BsonString str && str.Value.StartsWith('$'))
            {
                var path = str.Value[1..];
                return BsonValueHelper.ResolveSingle(document, path) ?? BsonNull.Value;
            }

            if (expression is BsonArray array)
                return new BsonArray(array.Select(item => Evaluate(item, document)));

            if (expression is BsonDocument doc)
            {
                if (doc.ElementCount == 1 && doc.GetElement(0).Name.StartsWith('$'))
                    return EvaluateOperator(doc.GetElement(0).Name, doc.GetElement(0).Value, document);

                var result = new BsonDocument();
                foreach (var element in doc.Elements)
                    result.Add(element.Name, Evaluate(element.Value, document));
                return result;
            }

            return expression;
        }

        static BsonValue EvaluateOperator(string name, BsonValue args, BsonDocument document)
        {
            switch (name)
            {
                case "$literal":
                    return args;
                case "$eq":
                case "$ne":
                case "$gt":
                case "$gte":
                case "$lt":
                case "$lte":
                    {
                        var operands = args.AsBsonArray;
                        var left = Evaluate(operands[0], document);
                        var right = Evaluate(operands[1], document);
                        // Aggregation comparisons use a total order across BSON types.
                        var comparison = left.CompareTo(right);
                        return name switch
                        {
                            "$eq" => comparison == 0,
                            "$ne" => comparison != 0,
                            "$gt" => comparison > 0,
                            "$gte" => comparison >= 0,
                            "$lt" => comparison < 0,
                            _ => comparison <= 0
                        };
                    }
                case "$and":
                    return args.AsBsonArray.All(item => BsonValueHelper.IsTruthy(Evaluate(item, document)));
                case "$or":
                    return args.AsBsonArray.Any(item => BsonValueHelper.IsTruthy(Evaluate(item, document)));
                case "$not":
                    {
                        var operand = args is BsonArray notArray ? notArray[0] : args;
                        return !BsonValueHelper.IsTruthy(Evaluate(operand, document));
                    }
                case "$in":
                    {
                        var operands = args.AsBsonArray;
                        var needle = Evaluate(operands[0], document);
                        var haystack = Evaluate(operands[1], document);
                        return haystack is BsonArray hayArray && hayArray.Any(item => BsonValueHelper.ValuesEqual(item, needle));
                    }
                case "$ifNull":
                    {
                        var operands = args.AsBsonArray;
                        var value = Evaluate(operands[0], document);
                        return value.IsBsonNull ? Evaluate(operands[1], document) : value;
                    }
                case "$cond":
                    {
                        BsonValue ifExpr, thenExpr, elseExpr;
                        if (args is BsonArray condArray)
                        {
                            ifExpr = condArray[0];
                            thenExpr = condArray[1];
                            elseExpr = condArray[2];
                        }
                        else
                        {
                            var condDoc = args.AsBsonDocument;
                            ifExpr = condDoc["if"];
                            thenExpr = condDoc["then"];
                            elseExpr = condDoc["else"];
                        }

                        return BsonValueHelper.IsTruthy(Evaluate(ifExpr, document))
                            ? Evaluate(thenExpr, document)
                            : Evaluate(elseExpr, document);
                    }
                default:
                    throw new NotSupportedException($"Aggregation expression operator \"{name}\" is not supported by the in-memory fake.");
            }
        }
    }
}
