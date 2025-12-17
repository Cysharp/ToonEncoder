#nullable enable
using ToonFormat;

namespace Toon.Format;

/// <summary>
/// Options for encoding data to TOON format.
/// </summary>
public class ToonEncodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// </summary>
    /// <remarks>Default is 2</remarks>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// Delimiter to use for tabular array rows and inline primitive arrays.
    /// Default is comma (,).
    /// </summary>
    /// <remarks>Default is <see cref="ToonDelimiter.COMMA"/></remarks>
    public ToonDelimiter Delimiter { get; set; } = Constants.DEFAULT_DELIMITER_ENUM;

    /// <summary>
    /// Enable key folding to collapse single-key wrapper chains.
    /// When set to <see cref="ToonKeyFolding.Safe"/>, nested objects with single keys are
    /// collapsed into dotted paths
    /// (e.g., data.metadata.items instead of nested indentation).
    /// </summary>
    /// <remarks>Default is <see cref="ToonKeyFolding.Off"/></remarks>
    public ToonKeyFolding KeyFolding { get; set; } = ToonKeyFolding.Off;

    /// <summary>
    /// Maximum number of segments to fold when <see cref="ToonEncodeOptions.KeyFolding"/> is enabled.
    /// Controls how deep the folding can go in single-key chains.
    /// Values 0 or 1 have no practical effect (treated as effectively disabled).
    /// </summary>
    /// <remarks>Default is <see cref="int.MaxValue"/></remarks>
    public int? FlattenDepth { get; set; } = int.MaxValue;
}
