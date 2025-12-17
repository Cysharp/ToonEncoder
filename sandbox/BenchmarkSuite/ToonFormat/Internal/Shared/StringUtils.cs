#nullable enable
using System.Text;

namespace ToonFormat.Internal.Shared
{
    /// <summary>
    /// String utilities, aligned with TypeScript version shared/string-utils.ts:
    /// - EscapeString: Escapes special characters during encoding
    /// - UnescapeString: Restores escape sequences during decoding
    /// - FindClosingQuote: Finds the position of the matching closing quote, considering escapes
    /// - FindUnquotedChar: Finds the position of the target character not inside quotes
    /// </summary>
    internal static class StringUtils
    {
        /// <summary>
        /// Escapes special characters: backslash, quotes, newlines, carriage returns, tabs.
        /// Equivalent to TS escapeString.
        /// </summary>
        internal static string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            return value
                .Replace("\\", $"{Constants.BACKSLASH}{Constants.BACKSLASH}")
                .Replace("\"", $"{Constants.BACKSLASH}{Constants.DOUBLE_QUOTE}")
                .Replace("\n", $"{Constants.BACKSLASH}n")
                .Replace("\r", $"{Constants.BACKSLASH}r")
                .Replace("\t", $"{Constants.BACKSLASH}t");
        }

        /// <summary>
        /// Unescapes the string, supporting \n, \t, \r, \\, \". Invalid sequences throw <see cref="ToonFormatException"/>.
        /// Equivalent to TS unescapeString.
        /// </summary>
        internal static string UnescapeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            var sb = new StringBuilder(value.Length);
            int i = 0;
            while (i < value.Length)
            {
                var ch = value[i];
                if (ch == Constants.BACKSLASH)
                {
                    if (i + 1 >= value.Length)
                        throw ToonFormatException.Syntax("Invalid escape sequence: backslash at end of string");

                    var next = value[i + 1];
                    switch (next)
                    {
                        case 'n':
                            sb.Append(Constants.NEWLINE);
                            i += 2;
                            continue;
                        case 't':
                            sb.Append(Constants.TAB);
                            i += 2;
                            continue;
                        case 'r':
                            sb.Append(Constants.CARRIAGE_RETURN);
                            i += 2;
                            continue;
                        case '\\':
                            sb.Append(Constants.BACKSLASH);
                            i += 2;
                            continue;
                        case '"':
                            sb.Append(Constants.DOUBLE_QUOTE);
                            i += 2;
                            continue;
                        default:
                            throw ToonFormatException.Syntax($"Invalid escape sequence: \\{next}");
                    }
                }

                sb.Append(ch);
                i++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Finds the position of the next double quote in the string starting from 'start', considering escapes.
        /// Returns -1 if not found. Equivalent to TS findClosingQuote.
        /// </summary>
        internal static int FindClosingQuote(string content, int start)
        {
            int i = start + 1;
            while (i < content.Length)
            {
                // Skip the next character when encountering an escape inside quotes
                if (content[i] == Constants.BACKSLASH && i + 1 < content.Length)
                {
                    i += 2;
                    continue;
                }

                if (content[i] == Constants.DOUBLE_QUOTE)
                    return i;

                i++;
            }
            return -1;
        }

        /// <summary>
        /// Finds the position of the target character not inside quotes; returns -1 if not found.
        /// Escape sequences inside quotes are skipped. Equivalent to TS findUnquotedChar.
        /// </summary>
        internal static int FindUnquotedChar(string content, char target, int start = 0)
        {
            bool inQuotes = false;
            int i = start;

            while (i < content.Length)
            {
                if (inQuotes && content[i] == Constants.BACKSLASH && i + 1 < content.Length)
                {
                    // Skip the next character for escape sequences inside quotes
                    i += 2;
                    continue;
                }

                if (content[i] == Constants.DOUBLE_QUOTE)
                {
                    inQuotes = !inQuotes;
                    i++;
                    continue;
                }

                if (!inQuotes && content[i] == target)
                    return i;

                i++;
            }

            return -1;
        }

        /// <summary>
        /// Generates a quoted string literal, escaping internal characters as necessary.
        /// Note: Whether quotes are needed should be determined by the caller based on ValidationShared rules.
        /// </summary>
        internal static string Quote(string value)
        {
            return $"\"{EscapeString(value)}\"";
        }
    }
}