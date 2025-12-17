#nullable enable
using System.Collections.Generic;
using System.Text;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Helper class for building indented lines of TOON output.
    /// Aligned with TypeScript encode/writer.ts
    /// </summary>
    internal class LineWriter
    {
        private readonly List<string> _lines = new();
        private readonly string _indentationString;

        /// <summary>
        /// Creates a new LineWriter with the specified indentation size.
        /// </summary>
        /// <param name="indentSize">Number of spaces per indentation level.</param>
        public LineWriter(int indentSize)
        {
            _indentationString = new string(' ', indentSize);
        }

        /// <summary>
        /// Pushes a new line with the specified depth and content.
        /// </summary>
        /// <param name="depth">Indentation depth level.</param>
        /// <param name="content">The content of the line.</param>
        public void Push(int depth, string content)
        {
            var indent = RepeatString(_indentationString, depth);
            _lines.Add(indent + content);
        }

        /// <summary>
        /// Pushes a list item (prefixed with "- ") at the specified depth.
        /// </summary>
        /// <param name="depth">Indentation depth level.</param>
        /// <param name="content">The content after the list item marker.</param>
        public void PushListItem(int depth, string content)
        {
            Push(depth, Constants.LIST_ITEM_PREFIX + content);
        }

        /// <summary>
        /// Returns the complete output as a single string with newlines.
        /// </summary>
        public override string ToString()
        {
            return string.Join("\n", _lines);
        }

        /// <summary>
        /// Helper method to repeat a string n times.
        /// </summary>
        private static string RepeatString(string str, int count)
        {
            if (count <= 0)
                return string.Empty;

            var sb = new StringBuilder(str.Length * count);
            for (int i = 0; i < count; i++)
            {
                sb.Append(str);
            }
            return sb.ToString();
        }
    }
}
