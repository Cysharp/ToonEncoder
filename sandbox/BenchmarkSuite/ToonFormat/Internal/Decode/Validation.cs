#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToonFormat.Internal.Decode
{
    /// <summary>
    /// Options for decoding TOON format.
    /// </summary>
    internal class ResolvedDecodeOptions
    {
        public int Indent { get; set; } = 2;
        public bool Strict { get; set; } = false;
    }

    /// <summary>
    /// Validation utilities for TOON decoding.
    /// Aligned with TypeScript decode/validation.ts
    /// </summary>
    internal static class Validation
    {
        /// <summary>
        /// Asserts that the actual count matches the expected count in strict mode.
        /// </summary>
        /// <param name="actual">The actual count</param>
        /// <param name="expected">The expected count</param>
        /// <param name="itemType">The type of items being counted (e.g., "list array items", "tabular rows")</param>
        /// <param name="options">Decode options</param>
        /// <exception cref="ToonFormatException">Thrown if counts don't match in strict mode</exception>
        public static void AssertExpectedCount(
            int actual,
            int expected,
            string itemType,
            ResolvedDecodeOptions options)
        {
            if (options.Strict && actual != expected)
            {
                throw ToonFormatException.Range($"Expected {expected} {itemType}, but got {actual}");
            }
        }

        /// <summary>
        /// Validates that there are no extra list items beyond the expected count.
        /// </summary>
        /// <param name="cursor">The line cursor</param>
        /// <param name="itemDepth">The expected depth of items</param>
        /// <param name="expectedCount">The expected number of items</param>
        /// <exception cref="ToonFormatException">Thrown if extra items are found</exception>
        public static void ValidateNoExtraListItems(
            LineCursor cursor,
            int itemDepth,
            int expectedCount)
        {
            if (cursor.AtEnd())
                return;

            var nextLine = cursor.Peek();
            if (nextLine != null && nextLine.Depth == itemDepth && nextLine.Content.StartsWith(Constants.LIST_ITEM_PREFIX))
            {
                throw ToonFormatException.Range($"Expected {expectedCount} list array items, but found more");
            }
        }

        /// <summary>
        /// Validates that there are no extra tabular rows beyond the expected count.
        /// </summary>
        /// <param name="cursor">The line cursor</param>
        /// <param name="rowDepth">The expected depth of rows</param>
        /// <param name="header">The array header info containing length and delimiter</param>
        /// <exception cref="ToonFormatException">Thrown if extra rows are found</exception>
        public static void ValidateNoExtraTabularRows(
            LineCursor cursor,
            int rowDepth,
            ArrayHeaderInfo header)
        {
            if (cursor.AtEnd())
                return;

            var nextLine = cursor.Peek();
            if (nextLine != null
                && nextLine.Depth == rowDepth
                && !nextLine.Content.StartsWith(Constants.LIST_ITEM_PREFIX)
                && IsDataRow(nextLine.Content, header.Delimiter))
            {
                throw ToonFormatException.Range($"Expected {header.Length} tabular rows, but found more");
            }
        }

        /// <summary>
        /// Validates that there are no blank lines within a specific line range and depth.
        /// </summary>
        /// <remarks>
        /// In strict mode, blank lines inside arrays/tabular rows are not allowed.
        /// </remarks>
        /// <param name="startLine">The starting line number (inclusive)</param>
        /// <param name="endLine">The ending line number (inclusive)</param>
        /// <param name="blankLines">Array of blank line information</param>
        /// <param name="strict">Whether strict mode is enabled</param>
        /// <param name="context">Description of the context (e.g., "list array", "tabular array")</param>
        /// <exception cref="ToonFormatException">Thrown if blank lines are found in strict mode</exception>
        public static void ValidateNoBlankLinesInRange(
            int startLine,
            int endLine,
            List<BlankLineInfo> blankLines,
            bool strict,
            string context)
        {
            if (!strict)
                return;

            // Find blank lines within the range
            // Note: We don't filter by depth because ANY blank line between array items is an error,
            // regardless of its indentation level
            var blanksInRange = blankLines.Where(
                blank => blank.LineNumber > startLine && blank.LineNumber < endLine
            ).ToList();

            if (blanksInRange.Count > 0)
            {
                throw ToonFormatException.Syntax(
                    $"Line {blanksInRange[0].LineNumber}: Blank lines inside {context} are not allowed in strict mode"
                );
            }
        }

        /// <summary>
        /// Checks if a line represents a data row (as opposed to a key-value pair) in a tabular array.
        /// </summary>
        /// <param name="content">The line content</param>
        /// <param name="delimiter">The delimiter used in the table</param>
        /// <returns>true if the line is a data row, false if it's a key-value pair</returns>
        private static bool IsDataRow(string content, char delimiter)
        {
            var colonPos = content.IndexOf(Constants.COLON);
            var delimiterPos = content.IndexOf(delimiter);

            // No colon = definitely a data row
            if (colonPos == -1)
                return true;

            // Has delimiter and it comes before colon = data row
            if (delimiterPos != -1 && delimiterPos < colonPos)
                return true;

            // Colon before delimiter or no delimiter = key-value pair
            return false;
        }
    }
}
