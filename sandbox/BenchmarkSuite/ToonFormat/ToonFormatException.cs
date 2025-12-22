#nullable enable
using System;
using System.Text;

namespace ToonFormat
{
    /// <summary>
    /// Exception thrown when TOON format parsing or encoding fails.
    /// </summary>
    public sealed class ToonFormatException : Exception
    {
        /// <summary>Error type (syntax, range, validation, indentation, delimiter, unknown).</summary>
        public ToonErrorKind Kind { get; }

        /// <summary>1-based line number.</summary>
        public int? LineNumber { get; }

        /// <summary>1-based column number.</summary>
        public int? ColumnNumber { get; }

        /// <summary>Original line text where the error occurred (may be truncated).</summary>
        public string? SourceLine { get; }

        /// <summary>Indentation depth (optional, for debugging).</summary>
        public int? Depth { get; }

        /// <summary>Constructs the exception, automatically concatenating messages with position and line context.</summary>
        public ToonFormatException(
            ToonErrorKind kind,
            string message,
            int? lineNumber = null,
            int? columnNumber = null,
            string? sourceLine = null,
            int? depth = null,
            Exception? inner = null)
            : base(BuildMessage(kind, message, lineNumber, columnNumber, sourceLine), inner)
        {
            Kind = kind;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            SourceLine = sourceLine;
            Depth = depth;
        }

        /// <summary>Syntax error factory method.</summary>
        public static ToonFormatException Syntax(
            string message,
            int? lineNumber = null,
            int? columnNumber = null,
            string? sourceLine = null,
            int? depth = null,
            Exception? inner = null)
            => new(ToonErrorKind.Syntax, message, lineNumber, columnNumber, sourceLine, depth, inner);

        /// <summary>Range error factory method (e.g., count mismatches).</summary>
        public static ToonFormatException Range(
            string message,
            int? lineNumber = null,
            int? columnNumber = null,
            string? sourceLine = null,
            int? depth = null,
            Exception? inner = null)
            => new(ToonErrorKind.Range, message, lineNumber, columnNumber, sourceLine, depth, inner);

        /// <summary>Validation error factory method (extra lines/empty lines/structural rules).</summary>
        public static ToonFormatException Validation(
            string message,
            int? lineNumber = null,
            int? columnNumber = null,
            string? sourceLine = null,
            int? depth = null,
            Exception? inner = null)
            => new(ToonErrorKind.Validation, message, lineNumber, columnNumber, sourceLine, depth, inner);

        /// <summary>Indentation error factory method (in strict mode, indentation must be multiples of indent and cannot contain TAB).</summary>
        public static ToonFormatException Indentation(
            string message,
            int? lineNumber = null,
            int? columnNumber = null,
            string? sourceLine = null,
            int? depth = null,
            Exception? inner = null)
            => new(ToonErrorKind.Indentation, message, lineNumber, columnNumber, sourceLine, depth, inner);

        /// <summary>Delimiter-related error factory method.</summary>
        public static ToonFormatException Delimiter(
            string message,
            int? lineNumber = null,
            int? columnNumber = null,
            string? sourceLine = null,
            int? depth = null,
            Exception? inner = null)
            => new(ToonErrorKind.Delimiter, message, lineNumber, columnNumber, sourceLine, depth, inner);

        private static string BuildMessage(
            ToonErrorKind kind,
            string message,
            int? lineNumber,
            int? columnNumber,
            string? sourceLine)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(kind).Append("] ").Append(message);

            if (lineNumber is not null)
                sb.Append(" (Line ").Append(lineNumber.Value).Append(')');
            if (columnNumber is not null)
                sb.Append(" (Column ").Append(columnNumber.Value).Append(')');

            if (!string.IsNullOrEmpty(sourceLine))
            {
                sb.AppendLine();
                sb.Append("  > ").Append(sourceLine);

                if (columnNumber is not null && columnNumber.Value > 0)
                {
                    sb.AppendLine();
                    sb.Append("    ");
                    // Caret pointing to the column position
                    var caretPos = Math.Max(1, columnNumber.Value);
                    sb.Append(new string(' ', caretPos - 1)).Append('^');
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>TOON error type classification.</summary>
    public enum ToonErrorKind
    {
        /// <summary>Syntax error: illegal tokens or structures encountered during scanning/parsing phase.</summary>
        Syntax,
        /// <summary>Range error: count mismatches (e.g., [N] does not match actual item count).</summary>
        Range,
        /// <summary>Validation error: structural/rule validation failure in strict mode (e.g., extra lines, empty lines).</summary>
        Validation,
        /// <summary>Indentation error: indentation is not a multiple of Indent or contains TAB.</summary>
        Indentation,
        /// <summary>Delimiter error: fields/values contain disallowed delimiters or delimiter inference failed.</summary>
        Delimiter,
        /// <summary>Unknown error: unclassified exceptions.</summary>
        Unknown
    }
}
