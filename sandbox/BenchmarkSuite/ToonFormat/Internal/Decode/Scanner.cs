#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToonFormat.Internal.Decode
{
    /// <summary>
    /// Represents a parsed line with its raw content, indentation, depth, and line number.
    /// </summary>
    internal class ParsedLine
    {
        public string Raw { get; set; } = string.Empty;
        public int Indent { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Depth { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Information about a blank line in the source.
    /// </summary>
    internal class BlankLineInfo
    {
        public int LineNumber { get; set; }
        public int Indent { get; set; }
        public int Depth { get; set; }
    }

    /// <summary>
    /// Result of scanning source text into parsed lines.
    /// </summary>
    internal class ScanResult
    {
        public List<ParsedLine> Lines { get; set; } = new();
        public List<BlankLineInfo> BlankLines { get; set; } = new();
    }

    /// <summary>
    /// Cursor for navigating through parsed lines during decoding.
    /// Aligned with TypeScript decode/scanner.ts LineCursor
    /// </summary>
    internal class LineCursor
    {
        private readonly List<ParsedLine> _lines;
        private readonly List<BlankLineInfo> _blankLines;
        private int _index;

        public LineCursor(List<ParsedLine> lines, List<BlankLineInfo> blankLines)
        {
            _lines = lines;
            _blankLines = blankLines;
            _index = 0;
        }

        public List<BlankLineInfo> GetBlankLines() => _blankLines;

        public ParsedLine? Peek()
        {
            return _index < _lines.Count ? _lines[_index] : null;
        }

        public ParsedLine? Next()
        {
            return _index < _lines.Count ? _lines[_index++] : null;
        }

        public ParsedLine? Current()
        {
            return _index > 0 ? _lines[_index - 1] : null;
        }

        public void Advance()
        {
            _index++;
        }

        public bool AtEnd()
        {
            return _index >= _lines.Count;
        }

        public int Length => _lines.Count;

        public ParsedLine? PeekAtDepth(int targetDepth)
        {
            var line = Peek();
            if (line == null || line.Depth < targetDepth)
                return null;
            if (line.Depth == targetDepth)
                return line;
            return null;
        }

        public bool HasMoreAtDepth(int targetDepth)
        {
            return PeekAtDepth(targetDepth) != null;
        }
    }

    /// <summary>
    /// Scanner utilities for parsing source text into structured lines.
    /// Aligned with TypeScript decode/scanner.ts
    /// </summary>
    internal static class Scanner
    {
        /// <summary>
        /// Parses source text into a list of structured lines with depth information.
        /// </summary>
        public static ScanResult ToParsedLines(string source, int indentSize, bool strict)
        {
            int estimatedLines = 1;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '\n')
                    estimatedLines++;
            }
            var parsed = new List<ParsedLine>(estimatedLines);
            var blankLines = new List<BlankLineInfo>(Math.Max(4, estimatedLines / 4));
            if (string.IsNullOrWhiteSpace(source))
            {
                return new ScanResult { Lines = parsed, BlankLines = blankLines };
            }
            ReadOnlySpan<char> span = source.AsSpan();
            int lineNumber = 0;
            while (!span.IsEmpty)
            {
                lineNumber++;
                // find the end of this line
                int newlineIdx = span.IndexOf('\n');
                ReadOnlySpan<char> lineSpan;
                if (newlineIdx >= 0)
                {
                    lineSpan = span.Slice(0, newlineIdx);
                    span = span.Slice(newlineIdx + 1);
                }
                else
                {
                    lineSpan = span;
                    span = ReadOnlySpan<char>.Empty;
                }
                // remove trailing carriage return if present
                if (!lineSpan.IsEmpty && lineSpan[lineSpan.Length - 1] == '\r')
                {
                    lineSpan = lineSpan.Slice(0, lineSpan.Length - 1);
                }
                // calculate indentation
                int indent = 0;
                while (indent < lineSpan.Length && lineSpan[indent] == Constants.SPACE)
                {
                    indent++;
                }
                ReadOnlySpan<char> contentSpan = lineSpan.Slice(indent);
                if (contentSpan.IsWhiteSpace())
                {
                    var depth = ComputeDepthFromIndent(indent, indentSize);
                    blankLines.Add(new BlankLineInfo
                    {
                        LineNumber = lineNumber,
                        Indent = indent,
                        Depth = depth
                    });
                    continue;
                }
                var lineDepth = ComputeDepthFromIndent(indent, indentSize);
                if (strict)
                {
                    int wsEnd = 0;
                    while (wsEnd < lineSpan.Length &&
                           (lineSpan[wsEnd] == Constants.SPACE || lineSpan[wsEnd] == Constants.TAB))
                    {
                        wsEnd++;
                    }
                    for (int j = 0; j < wsEnd; j++)
                    {
                        if (lineSpan[j] == Constants.TAB)
                        {
                            throw ToonFormatException.Syntax(
                                $"Line {lineNumber}: Tabs are not allowed in indentation in strict mode");
                        }
                    }
                    if (indent > 0 && indent % indentSize != 0)
                    {
                        throw ToonFormatException.Syntax(
                            $"Line {lineNumber}: Indentation must be exact multiple of {indentSize}, but found {indent} spaces");
                    }
                }
                parsed.Add(new ParsedLine
                {
                    Raw = new string(lineSpan),
                    Indent = indent,
                    Content = new string(contentSpan),
                    Depth = lineDepth,
                    LineNumber = lineNumber
                });
            }
            return new ScanResult { Lines = parsed, BlankLines = blankLines };
        }

        private static bool IsWhiteSpace(this ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(span[i]))
                    return false;
            }
            return true;
        }

        private static int ComputeDepthFromIndent(int indentSpaces, int indentSize)
        {
            return indentSpaces / indentSize;
        }
    }
}
