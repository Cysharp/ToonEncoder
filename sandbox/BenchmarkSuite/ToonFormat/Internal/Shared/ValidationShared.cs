#nullable enable
using System;
using System.Text.RegularExpressions;
using ToonFormat;

namespace ToonFormat.Internal.Shared
{
    /// <summary>
    /// Validation utilities aligned with TypeScript version shared/validation.ts:
    /// - IsValidUnquotedKey: Whether the key name can be without quotes
    /// - IsSafeUnquoted: Whether the string value can be without quotes
    /// - IsBooleanOrNullLiteral: Whether it is true/false/null
    /// - IsNumericLike: Whether it looks like numeric text (including leading zero integers)
    /// </summary>
    internal static class ValidationShared
    {
        private static readonly Regex ValidUnquotedKeyRegex = new(
            pattern: "^[A-Z_][\\w.]*$",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex IdentifierSegmentRegex = new(
            pattern: "^[A-Z_]\\w*$",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex NumericLikeRegex = new(
            pattern: "^-?\\d+(?:\\.\\d+)?(?:e[+-]?\\d+)?$",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex LeadingZeroIntegerRegex = new(
            pattern: "^0\\d+$",
            options: RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly char[] StructuralBracketsAndBraces =
        {
            Constants.OPEN_BRACKET,
            Constants.CLOSE_BRACKET,
            Constants.OPEN_BRACE,
            Constants.CLOSE_BRACE
        };

        private static readonly char[] ControlCharacters =
        {
            Constants.NEWLINE,
            Constants.CARRIAGE_RETURN,
            Constants.TAB
        };

        /// <summary>Whether the key name can be without quotes.</summary>
        internal static bool IsValidUnquotedKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return ValidUnquotedKeyRegex.IsMatch(key);
        }

        internal static bool IsIdentifierSegment(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return IdentifierSegmentRegex.IsMatch(key);
        }

        /// <summary>Whether the string value can be safely without quotes.</summary>
        internal static bool IsSafeUnquoted(string value, ToonDelimiter delimiter = Constants.DEFAULT_DELIMITER_ENUM)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                return false;

            if (LiteralUtils.IsBooleanOrNullLiteral(value) || IsNumericLike(value))
                return false;

            if (value.IndexOf(Constants.COLON) >= 0)
                return false;

            if (value.IndexOf(Constants.DOUBLE_QUOTE) >= 0 || value.IndexOf(Constants.BACKSLASH) >= 0)
                return false;

            if (value.IndexOfAny(StructuralBracketsAndBraces) >= 0)
                return false;

            if (value.IndexOfAny(ControlCharacters) >= 0)
                return false;

            var delimiterChar = Constants.ToDelimiterChar(delimiter);
            if (value.IndexOf(delimiterChar) >= 0)
                return false;

            if (value[0] == Constants.LIST_ITEM_MARKER)
                return false;

            return true;
        }

        private static bool IsNumericLike(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return NumericLikeRegex.IsMatch(value) || LeadingZeroIntegerRegex.IsMatch(value);
        }
    }
}
