using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToonEncoder.Tests;

// Generate from https://github.com/toon-format/spec/blob/main/tests/fixtures.schema.json

/// <summary>
/// TOON Test Fixture - language-agnostic test fixture format
/// </summary>
public sealed class ToonTestFixture
{
    /// <summary>TOON specification version these tests target (e.g., "1.0", "1.6")</summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>Test category: encode (JSON → TOON) or decode (TOON → JSON)</summary>
    [JsonPropertyName("category")]
    public required TestCategory Category { get; init; }

    /// <summary>Brief description of what this fixture file tests</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Array of test cases</summary>
    [JsonPropertyName("tests")]
    public required TestCase[] Tests { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<TestCategory>))]
public enum TestCategory
{
    [JsonStringEnumMemberName("encode")]
    Encode,

    [JsonStringEnumMemberName("decode")]
    Decode
}

public sealed class TestCase
{
    /// <summary>Descriptive test name explaining what is being validated</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Input value - JSON value for encode tests, TOON string for decode tests</summary>
    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }

    /// <summary>Expected output - TOON string for encode tests, JSON value for decode tests</summary>
    [JsonPropertyName("expected")]
    public required string Expected { get; init; }

    /// <summary>If true, this test expects an error to be thrown</summary>
    [JsonPropertyName("shouldError")]
    public bool ShouldError { get; init; }

    /// <summary>Encoding or decoding options</summary>
    [JsonPropertyName("options")]
    public TestOptions? Options { get; init; }

    /// <summary>Reference to relevant specification section (e.g., "6", "§7.2")</summary>
    [JsonPropertyName("specSection")]
    public string? SpecSection { get; init; }

    /// <summary>Optional note explaining special cases or edge case behavior</summary>
    [JsonPropertyName("note")]
    public string? Note { get; init; }

    /// <summary>Minimum specification version required for this test</summary>
    [JsonPropertyName("minSpecVersion")]
    public string? MinSpecVersion { get; init; }
}

public sealed class TestOptions
{
    /// <summary>Array delimiter (encode only): ",", "\t", or "|"</summary>
    [JsonPropertyName("delimiter")]
    public string? Delimiter { get; init; }

    /// <summary>Number of spaces per indentation level (default: 2)</summary>
    [JsonPropertyName("indent")]
    public int? Indent { get; init; }

    /// <summary>Enable strict validation (decode only, default: true)</summary>
    [JsonPropertyName("strict")]
    public bool? Strict { get; init; }

    /// <summary>Key folding mode for encoders (v1.5+): "off" or "safe"</summary>
    [JsonPropertyName("keyFolding")]
    public string? KeyFolding { get; init; }

    /// <summary>Maximum depth to fold key chains when keyFolding is "safe" (v1.5+)</summary>
    [JsonPropertyName("flattenDepth")]
    public int? FlattenDepth { get; init; }

    /// <summary>Path expansion mode for decoders (v1.5+): "off" or "safe"</summary>
    [JsonPropertyName("expandPaths")]
    public string? ExpandPaths { get; init; }
}
