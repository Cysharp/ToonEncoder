#nullable enable
using System;
using System.Globalization;

namespace ToonFormat.Internal.Shared
{
    /// <summary>
    /// Literal judgment utilities, aligned with TypeScript version shared/literal-utils.ts.
    /// - IsBooleanOrNullLiteral: Determines if it is true/false/null
    /// - IsNumericLiteral: Determines if it is a numeric literal, rejecting invalid leading zero forms
    /// </summary>
    internal static class LiteralUtils
    {
        /// <summary>
        /// Checks if the token is a boolean or null literal: true, false, null.
        /// Equivalent to TS: isBooleanOrNullLiteral
        /// </summary>
        internal static bool IsBooleanOrNullLiteral(string token)
        {
            return string.Equals(token, Constants.TRUE_LITERAL, StringComparison.Ordinal)
                || string.Equals(token, Constants.FALSE_LITERAL, StringComparison.Ordinal)
                || string.Equals(token, Constants.NULL_LITERAL, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the token is a valid numeric literal.
        /// Rules aligned with TS:
        /// - Rejects leading zeros (except "0" itself or decimals like "0.xxx")
        /// - Parses successfully and is a finite number (not NaN/Infinity)
        /// </summary>
        internal static bool IsNumericLiteral(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // Must not have leading zeros (except "0" itself or decimals like "0.5")
            if (token.Length > 1 && token[0] == '0' && token[1] != '.')
                return false;

            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                return false;

            return !double.IsNaN(num) && !double.IsInfinity(num);
        }
    }
}
