using System;

namespace ToonFormat
{

    public static class Constants
    {
        public const char LIST_ITEM_MARKER = '-';

        public const string LIST_ITEM_PREFIX = "- ";

        // #region Structural characters
        public const char COMMA = ',';
        public const char COLON = ':';
        public const char SPACE = ' ';
        public const char PIPE = '|';
        public const char HASH = '#';
        public const char DOT = '.';
        // #endregion

        // #region Brackets and braces
        public const char OPEN_BRACKET = '[';
        public const char CLOSE_BRACKET = ']';
        public const char OPEN_BRACE = '{';
        public const char CLOSE_BRACE = '}';
        // #endregion

        // #region Literals
        public const string NULL_LITERAL = "null";
        public const string TRUE_LITERAL = "true";
        public const string FALSE_LITERAL = "false";
        // #endregion

        // #region Escape/control characters
        public const char BACKSLASH = '\\';
        public const char DOUBLE_QUOTE = '"';
        public const char NEWLINE = '\n';
        public const char CARRIAGE_RETURN = '\r';
        public const char TAB = '\t';

        // #region Delimiter defaults and mapping
        public const ToonDelimiter DEFAULT_DELIMITER_ENUM = ToonDelimiter.COMMA;

        /// <summary>Default delimiter character (comma).</summary>
        public const char DEFAULT_DELIMITER_CHAR = COMMA;

        /// <summary>Maps delimiter enum values to their specific characters.</summary>
        public static char ToDelimiterChar(ToonDelimiter delimiter) => delimiter switch
        {
            ToonDelimiter.COMMA => COMMA,
            ToonDelimiter.TAB => TAB,
            ToonDelimiter.PIPE => PIPE,
            _ => COMMA
        };

        /// <summary>Maps delimiter characters to enum; unknown characters fall back to comma.</summary>
        public static ToonDelimiter FromDelimiterChar(char delimiter) => delimiter switch
        {
            COMMA => ToonDelimiter.COMMA,
            TAB => ToonDelimiter.TAB,
            PIPE => ToonDelimiter.PIPE,
            _ => ToonDelimiter.COMMA
        };

        /// <summary>Returns whether the character is a supported delimiter.</summary>
        public static bool IsDelimiterChar(char c) => c == COMMA || c == TAB || c == PIPE;

        /// <summary>Returns whether the character is a whitespace character (space or tab).</summary>
        public static bool IsWhitespace(char c) => c == SPACE || c == TAB;

        /// <summary>Returns whether the character is a structural character.</summary>
        public static bool IsStructural(char c)
            => c == COLON || c == OPEN_BRACKET || c == CLOSE_BRACKET || c == OPEN_BRACE || c == CLOSE_BRACE;
        // #endregion
    }

    /// <summary>
    /// TOON's unified options configuration, styled to align with System.Text.Json. Used to control indentation,
    /// delimiters, strict mode, length markers, and underlying JSON behavior.
    /// </summary>
    public enum ToonDelimiter
    {
        /// <summary>Comma ,</summary>
        COMMA,

        /// <summary>Tab \t</summary>
        TAB,

        /// <summary>Pipe |</summary>
        PIPE
    }

    /// <summary>
    /// Key folding options
    /// </summary>
    public enum ToonKeyFolding
    {
        /// <summary>Key folding disabled</summary>
        Off,

        /// <summary>Nested objects with single keys are collapsed into dotted paths</summary>
        Safe
    }

}
