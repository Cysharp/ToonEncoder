#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Primitive value encoding, key encoding, and header formatting utilities.
    /// Aligned with TypeScript encode/primitives.ts
    /// </summary>
    internal static class Primitives
    {
        // #region Primitive encoding

        /// <summary>
        /// Encodes a primitive JSON value (null, boolean, number, or string) to its TOON representation.
        /// </summary>
        public static string EncodePrimitive(JsonNode? value, char delimiter = Constants.COMMA)
        {
            if (value == null)
                return Constants.NULL_LITERAL;

            if (value is JsonValue jsonValue)
            {
                // Boolean
                if (jsonValue.TryGetValue<bool>(out var boolVal))
                    return boolVal ? Constants.TRUE_LITERAL : Constants.FALSE_LITERAL;

                // Number
                if (jsonValue.TryGetValue<int>(out var intVal))
                    return intVal.ToString(CultureInfo.InvariantCulture);

                if (jsonValue.TryGetValue<long>(out var longVal))
                    return longVal.ToString(CultureInfo.InvariantCulture);

                if (jsonValue.TryGetValue<double>(out var doubleVal))
                    return doubleVal.ToString("G17", CultureInfo.InvariantCulture);

                if (jsonValue.TryGetValue<decimal>(out var decimalVal))
                    return decimalVal.ToString(CultureInfo.InvariantCulture);

                // String
                if (jsonValue.TryGetValue<string>(out var strVal))
                    return EncodeStringLiteral(strVal ?? string.Empty, delimiter);
            }

            return Constants.NULL_LITERAL;
        }

        /// <summary>
        /// Encodes a string literal, adding quotes if necessary.
        /// </summary>
        public static string EncodeStringLiteral(string value, char delimiter = Constants.COMMA)
        {
            var delimiterEnum = Constants.FromDelimiterChar(delimiter);

            if (ValidationShared.IsSafeUnquoted(value, delimiterEnum))
            {
                return value;
            }

            var escaped = StringUtils.EscapeString(value);
            return $"{Constants.DOUBLE_QUOTE}{escaped}{Constants.DOUBLE_QUOTE}";
        }

        // #endregion

        // #region Key encoding

        /// <summary>
        /// Encodes a key, adding quotes if necessary.
        /// </summary>
        public static string EncodeKey(string key)
        {
            if (ValidationShared.IsValidUnquotedKey(key))
            {
                return key;
            }

            var escaped = StringUtils.EscapeString(key);
            return $"{Constants.DOUBLE_QUOTE}{escaped}{Constants.DOUBLE_QUOTE}";
        }

        // #endregion

        // #region Value joining

        /// <summary>
        /// Encodes and joins an array of primitive values with the specified delimiter.
        /// </summary>
        public static string EncodeAndJoinPrimitives(IEnumerable<JsonNode?> values, char delimiter = Constants.COMMA)
        {
            var encoded = values.Select(v => EncodePrimitive(v, delimiter));
            return string.Join(delimiter.ToString(), encoded);
        }

        // #endregion

        // #region Header formatters

        /// <summary>
        /// Formats an array header with optional key, length marker, delimiter, and field names.
        /// Examples:
        /// - "[3]:" for unnamed array of 3 items
        /// - "items[5]:" for named array
        /// - "users[#2]{name,age}:" for tabular format with length marker
        /// </summary>
        public static string FormatHeader(
            int length,
            string? key = null,
            IReadOnlyList<string>? fields = null,
            char? delimiter = null)
        {
            var delimiterChar = delimiter ?? Constants.DEFAULT_DELIMITER_CHAR;
            var header = string.Empty;

            // Add key if present
            if (!string.IsNullOrEmpty(key))
            {
                header += EncodeKey(key);
            }

            // Add array length with optional marker and delimiter
            var delimiterSuffix = delimiterChar != Constants.DEFAULT_DELIMITER_CHAR
                ? delimiterChar.ToString()
                : string.Empty;

            header += $"{Constants.OPEN_BRACKET}{length}{delimiterSuffix}{Constants.CLOSE_BRACKET}";

            // Add field names for tabular format
            if (fields != null && fields.Count > 0)
            {
                var quotedFields = fields.Select(EncodeKey);
                var fieldsStr = string.Join(delimiterChar.ToString(), quotedFields);
                header += $"{Constants.OPEN_BRACE}{fieldsStr}{Constants.CLOSE_BRACE}";
            }

            header += Constants.COLON;

            return header;
        }

        // #endregion
    }
}
