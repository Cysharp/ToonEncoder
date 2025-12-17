#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Normalization utilities for converting arbitrary .NET objects to JsonNode representations
    /// and type guards for JSON value classification.
    /// Aligned with TypeScript encode/normalize.ts
    /// </summary>
    internal static class Normalize
    {
        // #region Normalization (object → JsonNode)

        /// <summary>
        /// Normalizes an arbitrary .NET value to a JsonNode representation.
        /// Handles primitives, collections, dates, and custom objects.
        /// </summary>
        public static JsonNode? NormalizeValue(object? value)
        {
            // null
            if (value == null)
                return null;

            // Primitives: string, boolean
            if (value is string str)
                return JsonValue.Create(str);

            if (value is bool b)
                return JsonValue.Create(b);

            // Numbers: canonicalize -0 to +0, handle NaN and Infinity
            if (value is double d)
            {
                // Canonicalize signed zero via FloatUtils
                var dn = FloatUtils.NormalizeSignedZero(d);
                if (!double.IsFinite(dn))
                    return null;
                return JsonValue.Create(dn);
            }

            if (value is float f)
            {
                // Canonicalize signed zero via FloatUtils
                var fn = FloatUtils.NormalizeSignedZero(f);
                if (!float.IsFinite(fn))
                    return null;
                return JsonValue.Create(fn);
            }

            // Other numeric types
            if (value is int i) return JsonValue.Create(i);
            if (value is long l) return JsonValue.Create(l);
            if (value is decimal dec) return JsonValue.Create(dec);
            if (value is byte by) return JsonValue.Create(by);
            if (value is sbyte sb) return JsonValue.Create(sb);
            if (value is short sh) return JsonValue.Create(sh);
            if (value is ushort us) return JsonValue.Create(us);
            if (value is uint ui) return JsonValue.Create(ui);
            if (value is ulong ul) return JsonValue.Create(ul);

            // DateTime → ISO string
            if (value is DateTime dt)
                return JsonValue.Create(dt.ToString("O")); // ISO 8601 format

            if (value is DateTimeOffset dto)
                return JsonValue.Create(dto.ToString("O"));

            // Array/List → JsonArray
            if (value is IEnumerable enumerable && value is not string)
            {
                var jsonArray = new JsonArray();
                foreach (var item in enumerable)
                {
                    jsonArray.Add(NormalizeValue(item));
                }
                return jsonArray;
            }

            // Dictionary/Object → JsonObject
            if (value is IDictionary dict)
            {
                var jsonObject = new JsonObject();
                foreach (DictionaryEntry entry in dict)
                {
                    var key = entry.Key?.ToString() ?? string.Empty;
                    jsonObject[key] = NormalizeValue(entry.Value);
                }
                return jsonObject;
            }

            // Plain object → JsonObject via reflection
            if (IsPlainObject(value))
            {
                var jsonObject = new JsonObject();
                var type = value.GetType();
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var prop in properties.Where(prop => prop.CanRead))
                {
                    var propValue = prop.GetValue(value);
                    jsonObject[prop.Name] = NormalizeValue(propValue);
                }

                return jsonObject;
            }

            // Fallback: unsupported types → null
            return null;
        }

        /// <summary>
        /// Normalizes a value of generic type to a JsonNode representation.
        /// This overload aims to avoid an initial boxing for common value types.
        /// </summary>
        public static JsonNode? NormalizeValue<T>(T value)
        {
            // null
            if (value is null)
                return null;

            // Fast-path primitives without boxing
            switch (value)
            {
                case string s:
                    return JsonValue.Create(s);
                case bool b:
                    return JsonValue.Create(b);
                case int i:
                    return JsonValue.Create(i);
                case long l:
                    return JsonValue.Create(l);
                case double d:
                    if (BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits(-0.0)) return JsonValue.Create(0.0);
                    if (!double.IsFinite(d)) return null;
                    return JsonValue.Create(d);
                case float f:
                    if (BitConverter.SingleToInt32Bits(f) == BitConverter.SingleToInt32Bits(-0.0f)) return JsonValue.Create(0.0f);
                    if (!float.IsFinite(f)) return null;
                    return JsonValue.Create(f);
                case decimal dec:
                    return JsonValue.Create(dec);
                case byte by:
                    return JsonValue.Create(by);
                case sbyte sb:
                    return JsonValue.Create(sb);
                case short sh:
                    return JsonValue.Create(sh);
                case ushort us:
                    return JsonValue.Create(us);
                case uint ui:
                    return JsonValue.Create(ui);
                case ulong ul:
                    return JsonValue.Create(ul);
                case DateTime dt:
                    return JsonValue.Create(dt.ToString("O"));
                case DateTimeOffset dto:
                    return JsonValue.Create(dto.ToString("O"));
            }

            // Collections / dictionaries (pattern checks avoid boxing for value-type T that implement the interfaces)
            if (value is IEnumerable enumerable && value is not string)
            {
                var jsonArray = new JsonArray();
                foreach (var item in enumerable)
                {
                    jsonArray.Add(NormalizeValue(item));
                }
                return jsonArray;
            }

            if (value is IDictionary dict)
            {
                var jsonObject = new JsonObject();
                foreach (DictionaryEntry entry in dict)
                {
                    var key = entry.Key?.ToString() ?? string.Empty;
                    jsonObject[key] = NormalizeValue(entry.Value);
                }
                return jsonObject;
            }

            // Plain object via reflection (boxing for value types here is acceptable and rare)
            if (IsPlainObject(value!))
            {
                var jsonObject = new JsonObject();
                var type = value!.GetType();
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (prop.CanRead)
                    {
                        var propValue = prop.GetValue(value);
                        jsonObject[prop.Name] = NormalizeValue(propValue);
                    }
                }

                return jsonObject;
            }

            return null;
        }

        /// <summary>
        /// Determines if a value is a plain object (not a primitive, collection, or special type).
        /// </summary>
        private static bool IsPlainObject(object value)
        {
            if (value == null)
                return false;

            var type = value.GetType();

            // Exclude primitives, strings, and special types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return false;

            // Exclude collections
            if (typeof(IEnumerable).IsAssignableFrom(type))
                return false;

            // Accept class or struct types
            return type.IsClass || type.IsValueType;
        }

        // #endregion

        // #region Type guards

        /// <summary>
        /// Checks if a JsonNode is a primitive value (null, string, number, or boolean).
        /// </summary>
        public static bool IsJsonPrimitive(JsonNode? value)
        {
            if (value == null)
                return true;

            if (value is JsonValue jsonValue)
            {
                // Check if it's a primitive type
                return jsonValue.TryGetValue<string>(out var a)
                    || jsonValue.TryGetValue<bool>(out var b)
                    || jsonValue.TryGetValue<int>(out var c)
                    || jsonValue.TryGetValue<long>(out var d)
                    || jsonValue.TryGetValue<double>(out var e)
                    || jsonValue.TryGetValue<decimal>(out var f);
            }

            return false;
        }

        /// <summary>
        /// Checks if a JsonNode is a JsonArray.
        /// </summary>
        public static bool IsJsonArray(JsonNode? value)
        {
            return value is JsonArray;
        }

        /// <summary>
        /// Checks if a JsonNode is a JsonObject.
        /// </summary>
        public static bool IsJsonObject(JsonNode? value)
        {
            return value is JsonObject;
        }

        /// <summary>
        /// Checks if a <see cref="JsonNode"/> is an object which is empty with no keys.
        /// </summary>
        /// <param name="value">The <see cref="JsonObject"/></param>
        /// <returns><see langword="true"/> if empty, <see langword="false"/> if not.</returns>
        public static bool IsEmptyObject(JsonNode? value)
        {
            return IsJsonObject(value) && (value as IDictionary<string, JsonNode>)?.Keys?.Count == 0;
        }

        // #endregion

        // #region Array type detection

        /// <summary>
        /// Checks if a JsonArray contains only primitive values.
        /// </summary>
        public static bool IsArrayOfPrimitives(JsonArray array)
        {
            return array.All(item => IsJsonPrimitive(item));
        }

        /// <summary>
        /// Checks if a JsonArray contains only arrays.
        /// </summary>
        public static bool IsArrayOfArrays(JsonArray array)
        {
            return array.All(item => IsJsonArray(item));
        }

        /// <summary>
        /// Checks if a JsonArray contains only objects.
        /// </summary>
        public static bool IsArrayOfObjects(JsonArray array)
        {
            return array.All(item => IsJsonObject(item));
        }

        // #endregion
    }
}
