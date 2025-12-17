#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Decode
{
    /// <summary>
    /// Main decoding functions for converting TOON format to JSON values.
    /// Aligned with TypeScript decode/decoders.ts
    /// </summary>
    internal static class Decoders
    {
        // #region Entry decoding

        /// <summary>
        /// Decodes TOON content from a line cursor into a JSON value.
        /// </summary>
        public static JsonNode? DecodeValueFromLines(LineCursor cursor, ResolvedDecodeOptions options)
        {
            var first = cursor.Peek();
            if (first == null)
            {
                throw ToonFormatException.Syntax("No content to decode");
            }

            // Check for root array
            if (Parser.IsArrayHeaderAfterHyphen(first.Content))
            {
                var headerInfo = Parser.ParseArrayHeaderLine(first.Content, Constants.DEFAULT_DELIMITER_CHAR);
                if (headerInfo != null)
                {
                    cursor.Advance(); // Move past the header line
                    return DecodeArrayFromHeader(headerInfo.Header, headerInfo.InlineValues, cursor, 0, options);
                }
            }

            // Check for single primitive value
            if (cursor.Length == 1 && !IsKeyValueLine(first))
            {
                return Parser.ParsePrimitiveToken(first.Content.Trim());
            }

            // Default to object
            return DecodeObject(cursor, 0, options);
        }

        private static bool IsKeyValueLine(ParsedLine line)
        {
            var content = line.Content;
            // Look for unquoted colon or quoted key followed by colon
            if (content.StartsWith("\""))
            {
                // Quoted key - find the closing quote
                var closingQuoteIndex = StringUtils.FindClosingQuote(content, 0);
                if (closingQuoteIndex == -1)
                    return false;

                // Check if colon exists after quoted key (may have array/brace syntax between)
                return content.Substring(closingQuoteIndex + 1).Contains(Constants.COLON);
            }
            else
            {
                // Unquoted key - look for first colon not inside quotes
                return content.Contains(Constants.COLON);
            }
        }

        // #endregion

        // #region Object decoding

        private static JsonObject DecodeObject(LineCursor cursor, int baseDepth, ResolvedDecodeOptions options)
        {
            var obj = new JsonObject();

            // Detect the actual depth of the first field (may differ from baseDepth in nested structures)
            int? computedDepth = null;

            while (!cursor.AtEnd())
            {
                var line = cursor.Peek();
                if (line == null || line.Depth < baseDepth)
                    break;

                if (computedDepth == null && line.Depth >= baseDepth)
                {
                    computedDepth = line.Depth;
                }

                if (line.Depth == computedDepth)
                {
                    var (key, value) = DecodeKeyValuePair(line, cursor, computedDepth.Value, options);
                    obj[key] = value;
                }
                else
                {
                    // Different depth (shallower or deeper) - stop object parsing
                    break;
                }
            }

            return obj;
        }

        private class KeyValueDecodeResult
        {
            public string Key { get; set; } = string.Empty;
            public JsonNode? Value { get; set; }
            public int FollowDepth { get; set; }
        }

        private static KeyValueDecodeResult DecodeKeyValue(
            string content,
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            // Check for array header first (before parsing key)
            var arrayHeader = Parser.ParseArrayHeaderLine(content, Constants.DEFAULT_DELIMITER_CHAR);
            if (arrayHeader != null && arrayHeader.Header.Key != null)
            {
                var value = DecodeArrayFromHeader(arrayHeader.Header, arrayHeader.InlineValues, cursor, baseDepth, options);
                // After an array, subsequent fields are at baseDepth + 1 (where array content is)
                return new KeyValueDecodeResult
                {
                    Key = arrayHeader.Header.Key,
                    Value = value,
                    FollowDepth = baseDepth + 1
                };
            }

            // Regular key-value pair
            var keyResult = Parser.ParseKeyToken(content, 0);
            var rest = content.Substring(keyResult.End).Trim();

            // No value after colon - expect nested object or empty
            if (string.IsNullOrEmpty(rest))
            {
                var nextLine = cursor.Peek();
                if (nextLine != null && nextLine.Depth > baseDepth)
                {
                    var nested = DecodeObject(cursor, baseDepth + 1, options);
                    return new KeyValueDecodeResult { Key = keyResult.Key, Value = nested, FollowDepth = baseDepth + 1 };
                }
                // Empty object
                return new KeyValueDecodeResult { Key = keyResult.Key, Value = new JsonObject(), FollowDepth = baseDepth + 1 };
            }

            // Inline primitive value
            var primitiveValue = Parser.ParsePrimitiveToken(rest);
            return new KeyValueDecodeResult { Key = keyResult.Key, Value = primitiveValue, FollowDepth = baseDepth + 1 };
        }

        private static (string key, JsonNode? value) DecodeKeyValuePair(
            ParsedLine line,
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            cursor.Advance();
            var result = DecodeKeyValue(line.Content, cursor, baseDepth, options);
            return (result.Key, result.Value);
        }

        // #endregion

        // #region Array decoding

        private static JsonNode DecodeArrayFromHeader(
            ArrayHeaderInfo header,
            string? inlineValues,
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            // Inline primitive array
            if (inlineValues != null)
            {
                // For inline arrays, cursor should already be advanced or will be by caller
                return new JsonArray(DecodeInlinePrimitiveArray(header, inlineValues, options).ToArray());
            }

            // For multi-line arrays (tabular or list), the cursor should already be positioned
            // at the array header line, but we haven't advanced past it yet

            // Tabular array
            if (header.Fields != null && header.Fields.Count > 0)
            {
                var tabularResult = DecodeTabularArray(header, cursor, baseDepth, options);
                return new JsonArray(tabularResult.Cast<JsonNode?>().ToArray());
            }

            // List array
            var listResult = DecodeListArray(header, cursor, baseDepth, options);
            return new JsonArray(listResult.ToArray());
        }

        private static List<JsonNode?> DecodeInlinePrimitiveArray(
            ArrayHeaderInfo header,
            string inlineValues,
            ResolvedDecodeOptions options)
        {
            if (string.IsNullOrWhiteSpace(inlineValues))
            {
                Validation.AssertExpectedCount(0, header.Length, "inline array items", options);
                return new List<JsonNode?>();
            }

            var values = Parser.ParseDelimitedValues(inlineValues, header.Delimiter);
            var primitives = Parser.MapRowValuesToPrimitives(values);

            Validation.AssertExpectedCount(primitives.Count, header.Length, "inline array items", options);

            return primitives;
        }

        private static List<JsonNode?> DecodeListArray(
            ArrayHeaderInfo header,
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            var items = new List<JsonNode?>();
            var itemDepth = baseDepth + 1;

            // Track line range for blank line validation
            int? startLine = null;
            int? endLine = null;

            while (!cursor.AtEnd() && items.Count < header.Length)
            {
                var line = cursor.Peek();
                if (line == null || line.Depth < itemDepth)
                    break;

                // Check for list item (with or without space after hyphen)
                var isListItem = line.Content.StartsWith(Constants.LIST_ITEM_PREFIX) || line.Content == "-";

                if (line.Depth == itemDepth && isListItem)
                {
                    // Track first and last item line numbers
                    if (startLine == null)
                        startLine = line.LineNumber;
                    endLine = line.LineNumber;

                    var item = DecodeListItem(cursor, itemDepth, options);
                    items.Add(item);

                    // Update endLine to the current cursor position (after item was decoded)
                    var currentLine = cursor.Current();
                    if (currentLine != null)
                        endLine = currentLine.LineNumber;
                }
                else
                {
                    break;
                }
            }

            Validation.AssertExpectedCount(items.Count, header.Length, "list array items", options);

            // In strict mode, check for blank lines inside the array
            if (options.Strict && startLine != null && endLine != null)
            {
                Validation.ValidateNoBlankLinesInRange(
                    startLine.Value,
                    endLine.Value,
                    cursor.GetBlankLines(),
                    options.Strict,
                    "list array"
                );
            }

            // In strict mode, check for extra items
            if (options.Strict)
            {
                Validation.ValidateNoExtraListItems(cursor, itemDepth, header.Length);
            }

            return items;
        }

        private static List<JsonObject> DecodeTabularArray(
            ArrayHeaderInfo header,
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            var objects = new List<JsonObject>();
            var rowDepth = baseDepth + 1;

            // Track line range for blank line validation
            int? startLine = null;
            int? endLine = null;

            while (!cursor.AtEnd() && objects.Count < header.Length)
            {
                var line = cursor.Peek();
                if (line == null || line.Depth < rowDepth)
                    break;

                if (line.Depth == rowDepth)
                {
                    // Track first and last row line numbers
                    if (startLine == null)
                        startLine = line.LineNumber;
                    endLine = line.LineNumber;

                    cursor.Advance();
                    var values = Parser.ParseDelimitedValues(line.Content, header.Delimiter);
                    Validation.AssertExpectedCount(values.Count, header.Fields!.Count, "tabular row values", options);

                    var primitives = Parser.MapRowValuesToPrimitives(values);
                    var obj = new JsonObject();

                    for (int i = 0; i < header.Fields!.Count; i++)
                    {
                        obj[header.Fields[i]] = primitives[i];
                    }

                    objects.Add(obj);
                }
                else
                {
                    break;
                }
            }

            Validation.AssertExpectedCount(objects.Count, header.Length, "tabular rows", options);

            // In strict mode, check for blank lines inside the array
            if (options.Strict && startLine != null && endLine != null)
            {
                Validation.ValidateNoBlankLinesInRange(
                    startLine.Value,
                    endLine.Value,
                    cursor.GetBlankLines(),
                    options.Strict,
                    "tabular array"
                );
            }

            // In strict mode, check for extra rows
            if (options.Strict)
            {
                Validation.ValidateNoExtraTabularRows(cursor, rowDepth, header);
            }

            return objects;
        }

        // #endregion

        // #region List item decoding

        private static JsonNode? DecodeListItem(
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            var line = cursor.Next();
            if (line == null)
            {
                throw ToonFormatException.Syntax("Expected list item");
            }

            // Check for list item (with or without space after hyphen)
            string afterHyphen;

            // Empty list item should be an empty object
            if (line.Content == "-")
            {
                return new JsonObject();
            }
            else if (line.Content.StartsWith(Constants.LIST_ITEM_PREFIX))
            {
                afterHyphen = line.Content.Substring(Constants.LIST_ITEM_PREFIX.Length);
            }
            else
            {
                throw ToonFormatException.Syntax($"Expected list item to start with \"{Constants.LIST_ITEM_PREFIX}\"");
            }

            // Empty content after list item should also be an empty object
            if (string.IsNullOrWhiteSpace(afterHyphen))
            {
                return new JsonObject();
            }

            // Check for array header after hyphen
            if (Parser.IsArrayHeaderAfterHyphen(afterHyphen))
            {
                var arrayHeader = Parser.ParseArrayHeaderLine(afterHyphen, Constants.DEFAULT_DELIMITER_CHAR);
                if (arrayHeader != null)
                {
                    return DecodeArrayFromHeader(arrayHeader.Header, arrayHeader.InlineValues, cursor, baseDepth, options);
                }
            }

            // Check for object first field after hyphen
            if (Parser.IsObjectFirstFieldAfterHyphen(afterHyphen))
            {
                return DecodeObjectFromListItem(line, cursor, baseDepth, options);
            }

            // Primitive value
            return Parser.ParsePrimitiveToken(afterHyphen);
        }

        private static JsonObject DecodeObjectFromListItem(
            ParsedLine firstLine,
            LineCursor cursor,
            int baseDepth,
            ResolvedDecodeOptions options)
        {
            var afterHyphen = firstLine.Content.Substring(Constants.LIST_ITEM_PREFIX.Length);
            var firstField = DecodeKeyValue(afterHyphen, cursor, baseDepth, options);

            var obj = new JsonObject { [firstField.Key] = firstField.Value };

            // Read subsequent fields
            while (!cursor.AtEnd())
            {
                var line = cursor.Peek();
                if (line == null || line.Depth < firstField.FollowDepth)
                    break;

                if (line.Depth == firstField.FollowDepth && !line.Content.StartsWith(Constants.LIST_ITEM_PREFIX))
                {
                    var (k, v) = DecodeKeyValuePair(line, cursor, firstField.FollowDepth, options);
                    obj[k] = v;
                }
                else
                {
                    break;
                }
            }

            return obj;
        }

        // #endregion
    }
}
