#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Decode
{
    /// <summary>
    /// Information about an array header.
    /// </summary>
    internal class ArrayHeaderInfo
    {
        public string? Key { get; set; }
        public int Length { get; set; }
        public char Delimiter { get; set; }
        public List<string>? Fields { get; set; }
    }

    /// <summary>
    /// Result of parsing an array header line.
    /// </summary>
    internal class ArrayHeaderParseResult
    {
        public ArrayHeaderInfo Header { get; set; } = null!;
        public string? InlineValues { get; set; }
    }

    /// <summary>
    /// Parsing utilities for TOON format tokens, headers, and values.
    /// Aligned with TypeScript decode/parser.ts
    /// </summary>
    internal static class Parser
    {
        // #region Array header parsing

        /// <summary>
        /// Parses an array header line like "key[3]:" or "users[#2,]{name,age}:".
        /// </summary>
        public static ArrayHeaderParseResult? ParseArrayHeaderLine(string content, char defaultDelimiter)
        {
            var trimmed = content.TrimStart();

            // Find the bracket segment, accounting for quoted keys that may contain brackets
            int bracketStart = -1;

            // For quoted keys, find bracket after closing quote (not inside the quoted string)
            if (trimmed.StartsWith(Constants.DOUBLE_QUOTE))
            {
                var closingQuoteIndex = StringUtils.FindClosingQuote(trimmed, 0);
                if (closingQuoteIndex == -1)
                    return null;

                var afterQuote = trimmed.Substring(closingQuoteIndex + 1);
                if (!afterQuote.StartsWith(Constants.OPEN_BRACKET.ToString()))
                    return null;

                // Calculate position in original content and find bracket after the quoted key
                var leadingWhitespace = content.Length - trimmed.Length;
                var keyEndIndex = leadingWhitespace + closingQuoteIndex + 1;
                bracketStart = content.IndexOf(Constants.OPEN_BRACKET, keyEndIndex);
            }
            else
            {
                // Unquoted key - find first bracket
                bracketStart = content.IndexOf(Constants.OPEN_BRACKET);
            }

            if (bracketStart == -1)
                return null;

            var bracketEnd = content.IndexOf(Constants.CLOSE_BRACKET, bracketStart);
            if (bracketEnd == -1)
                return null;

            // Find the colon that comes after all brackets and braces
            int colonIndex = bracketEnd + 1;
            int braceEnd = colonIndex;

            // Check for fields segment (braces come after bracket)
            var braceStart = content.IndexOf(Constants.OPEN_BRACE, bracketEnd);
            if (braceStart != -1 && braceStart < content.IndexOf(Constants.COLON, bracketEnd))
            {
                var foundBraceEnd = content.IndexOf(Constants.CLOSE_BRACE, braceStart);
                if (foundBraceEnd != -1)
                {
                    braceEnd = foundBraceEnd + 1;
                }
            }

            // Now find colon after brackets and braces
            colonIndex = content.IndexOf(Constants.COLON, Math.Max(bracketEnd, braceEnd));
            if (colonIndex == -1)
                return null;

            // Extract and parse the key (might be quoted)
            string? key = null;
            if (bracketStart > 0)
            {
                var rawKey = content.Substring(0, bracketStart).Trim();
                key = rawKey.StartsWith(Constants.DOUBLE_QUOTE.ToString())
                    ? ParseStringLiteral(rawKey)
                    : rawKey;
            }

            var afterColon = content.Substring(colonIndex + 1).Trim();
            var bracketContent = content.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);

            // Try to parse bracket segment
            BracketSegmentResult parsedBracket;
            try
            {
                parsedBracket = ParseBracketSegment(bracketContent, defaultDelimiter);
            }
            catch
            {
                return null;
            }

            // Check for fields segment
            List<string>? fields = null;
            if (braceStart != -1 && braceStart < colonIndex)
            {
                var foundBraceEnd = content.IndexOf(Constants.CLOSE_BRACE, braceStart);
                if (foundBraceEnd != -1 && foundBraceEnd < colonIndex)
                {
                    var fieldsContent = content.Substring(braceStart + 1, foundBraceEnd - braceStart - 1);
                    fields = ParseDelimitedValues(fieldsContent, parsedBracket.Delimiter)
                        .Select(field => ParseStringLiteral(field.Trim()))
                        .ToList();
                }
            }

            return new ArrayHeaderParseResult
            {
                Header = new ArrayHeaderInfo
                {
                    Key = key,
                    Length = parsedBracket.Length,
                    Delimiter = parsedBracket.Delimiter,
                    Fields = fields,
                },
                InlineValues = string.IsNullOrEmpty(afterColon) ? null : afterColon
            };
        }

        private class BracketSegmentResult
        {
            public int Length { get; set; }
            public char Delimiter { get; set; }
        }

        private static BracketSegmentResult ParseBracketSegment(string seg, char defaultDelimiter)
        {
            var content = seg;

            // Check for delimiter suffix
            char delimiter = defaultDelimiter;
            if (content.EndsWith(Constants.TAB.ToString()))
            {
                delimiter = Constants.TAB;
                content = content.Substring(0, content.Length - 1);
            }
            else if (content.EndsWith(Constants.PIPE.ToString()))
            {
                delimiter = Constants.PIPE;
                content = content.Substring(0, content.Length - 1);
            }

            if (!int.TryParse(content, out var length))
            {
                throw new FormatException($"Invalid array length: {seg}");
            }

            return new BracketSegmentResult
            {
                Length = length,
                Delimiter = delimiter,
            };
        }

        // #endregion

        // #region Delimited value parsing

        /// <summary>
        /// Parses a delimiter-separated string into individual values, respecting quotes.
        /// </summary>
        public static List<string> ParseDelimitedValues(string input, char delimiter)
        {
            var values = new List<string>(16); // pre-allocate for performance
            var current = new System.Text.StringBuilder(input.Length);
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];

                if (ch == Constants.BACKSLASH && inQuotes && i + 1 < input.Length)
                {
                    // Escape sequence in quoted string
                    current.Append(ch);
                    current.Append(input[i + 1]);
                    i++;
                    continue;
                }

                if (ch == Constants.DOUBLE_QUOTE)
                {
                    inQuotes = !inQuotes;
                    current.Append(ch);
                    continue;
                }

                if (ch == delimiter && !inQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0 || values.Count > 0)
            {
                values.Add(current.ToString().Trim());
            }

            return values;
        }

        /// <summary>
        /// Maps an array of string tokens to JSON primitive values.
        /// </summary>
        public static List<JsonNode?> MapRowValuesToPrimitives(List<string> values)
        {
            return values.Select(v => ParsePrimitiveToken(v)).ToList();
        }

        // #endregion

        // #region Primitive and key parsing

        /// <summary>
        /// Parses a primitive token (null, boolean, number, or string).
        /// </summary>
        public static JsonNode? ParsePrimitiveToken(string token)
        {
            var trimmed = token.Trim();

            // Empty token
            if (string.IsNullOrEmpty(trimmed))
                return JsonValue.Create(string.Empty);

            // Quoted string (if starts with quote, it MUST be properly quoted)
            if (trimmed.StartsWith(Constants.DOUBLE_QUOTE.ToString()))
            {
                return JsonValue.Create(ParseStringLiteral(trimmed));
            }

            // Boolean or null literals
            if (LiteralUtils.IsBooleanOrNullLiteral(trimmed))
            {
                if (trimmed == Constants.TRUE_LITERAL)
                    return JsonValue.Create(true);
                if (trimmed == Constants.FALSE_LITERAL)
                    return JsonValue.Create(false);
                if (trimmed == Constants.NULL_LITERAL)
                    return null;
            }

            // Numeric literal
            if (LiteralUtils.IsNumericLiteral(trimmed))
            {
                var parsedNumber = double.Parse(trimmed, CultureInfo.InvariantCulture);
                parsedNumber = FloatUtils.NormalizeSignedZero(parsedNumber);
                return JsonValue.Create(parsedNumber);
            }

            // Unquoted string
            return JsonValue.Create(trimmed);
        }

        /// <summary>
        /// Parses a string literal, handling quotes and escape sequences.
        /// </summary>
        public static string ParseStringLiteral(string token)
        {
            var trimmedToken = token.Trim();

            if (trimmedToken.StartsWith(Constants.DOUBLE_QUOTE.ToString()))
            {
                // Find the closing quote, accounting for escaped quotes
                var closingQuoteIndex = StringUtils.FindClosingQuote(trimmedToken, 0);

                if (closingQuoteIndex == -1)
                {
                    throw ToonFormatException.Syntax("Unterminated string: missing closing quote");
                }

                if (closingQuoteIndex != trimmedToken.Length - 1)
                {
                    throw ToonFormatException.Syntax("Unexpected characters after closing quote");
                }

                var content = trimmedToken.Substring(1, closingQuoteIndex - 1);
                return StringUtils.UnescapeString(content);
            }

            return trimmedToken;
        }

        public class KeyParseResult
        {
            public string Key { get; set; } = string.Empty;
            public int End { get; set; }
        }

        public static KeyParseResult ParseUnquotedKey(string content, int start)
        {
            int end = start;
            while (end < content.Length && content[end] != Constants.COLON)
            {
                end++;
            }

            // Validate that a colon was found
            if (end >= content.Length || content[end] != Constants.COLON)
            {
                throw ToonFormatException.Syntax("Missing colon after key");
            }

            var key = content.Substring(start, end - start).Trim();

            // Skip the colon
            end++;

            return new KeyParseResult { Key = key, End = end };
        }

        public static KeyParseResult ParseQuotedKey(string content, int start)
        {
            // Find the closing quote, accounting for escaped quotes
            var closingQuoteIndex = StringUtils.FindClosingQuote(content, start);

            if (closingQuoteIndex == -1)
            {
                throw ToonFormatException.Syntax("Unterminated quoted key");
            }

            // Extract and unescape the key content
            var keyContent = content.Substring(start + 1, closingQuoteIndex - start - 1);
            var key = StringUtils.UnescapeString(keyContent);
            int end = closingQuoteIndex + 1;

            // Validate and skip colon after quoted key
            if (end >= content.Length || content[end] != Constants.COLON)
            {
                throw ToonFormatException.Syntax("Missing colon after key");
            }
            end++;

            return new KeyParseResult { Key = key, End = end };
        }

        /// <summary>
        /// Parses a key token (quoted or unquoted) and returns the key and position after colon.
        /// </summary>
        public static KeyParseResult ParseKeyToken(string content, int start)
        {
            if (content[start] == Constants.DOUBLE_QUOTE)
            {
                return ParseQuotedKey(content, start);
            }
            else
            {
                return ParseUnquotedKey(content, start);
            }
        }

        // #endregion

        // #region Array content detection helpers

        /// <summary>
        /// Checks if content after hyphen starts with an array header.
        /// </summary>
        public static bool IsArrayHeaderAfterHyphen(string content)
        {
            return content.Trim().StartsWith(Constants.OPEN_BRACKET.ToString())
                && StringUtils.FindUnquotedChar(content, Constants.COLON) != -1;
        }

        /// <summary>
        /// Checks if content after hyphen contains a key-value pair (has a colon).
        /// </summary>
        public static bool IsObjectFirstFieldAfterHyphen(string content)
        {
            return StringUtils.FindUnquotedChar(content, Constants.COLON) != -1;
        }

        // #endregion
    }
}
