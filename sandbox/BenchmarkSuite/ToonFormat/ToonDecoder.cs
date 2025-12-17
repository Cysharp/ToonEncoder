#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ToonFormat;
using ToonFormat.Internal.Decode;

namespace Toon.Format;

/// <summary>
/// Decodes TOON-formatted strings into data structures.
/// </summary>
public static class ToonDecoder
{
    /// <summary>
    /// Decodes a TOON-formatted string into a JsonNode with default options.
    /// </summary>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <returns>The decoded JsonNode object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static JsonNode? Decode(string toonString)
    {
        return Decode(toonString, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes a TOON-formatted string into the specified type with default options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <returns>The deserialized value of type T.</returns>
    public static T? Decode<T>(string toonString)
    {
        return Decode<T>(toonString, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes a TOON-formatted string into a JsonNode with custom options.
    /// </summary>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <returns>The decoded JsonNode object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString or options is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static JsonNode? Decode(string toonString, ToonDecodeOptions? options)
    {
        if (toonString == null)
            throw new ArgumentNullException(nameof(toonString));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Resolve options
        var resolvedOptions = new ResolvedDecodeOptions
        {
            Indent = options.Indent,
            Strict = options.Strict
        };

        // Scan the source text into structured lines
        var scanResult = Scanner.ToParsedLines(toonString, resolvedOptions.Indent, resolvedOptions.Strict);

        // Handle empty input
        if (scanResult.Lines.Count == 0)
        {
            return new JsonObject();
        }

        // Create cursor and decode
        var cursor = new LineCursor(scanResult.Lines, scanResult.BlankLines);
        return Decoders.DecodeValueFromLines(cursor, resolvedOptions);
    }

    /// <summary>
    /// Decodes a TOON-formatted string into the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <returns>The deserialized value of type T.</returns>
    public static T? Decode<T>(string toonString, ToonDecodeOptions? options)
    {
        var node = Decode(toonString, options);
        if (node is null)
            return default;

        // If T is JsonNode or derived, return directly
        if (typeof(JsonNode).IsAssignableFrom(typeof(T)))
        {
            return (T?)(object?)node;
        }

        // Convert JsonNode -> JSON -> T using System.Text.Json
        var json = node.ToJsonString();
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Decodes TOON data from a UTF-8 byte array into a JsonNode with default options.
    /// </summary>
    /// <param name="utf8Bytes">UTF-8 encoded TOON text.</param>
    /// <returns>The decoded JsonNode object.</returns>
    public static JsonNode? Decode(byte[] utf8Bytes)
    {
        return Decode(utf8Bytes, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a UTF-8 byte array into a JsonNode with custom options.
    /// </summary>
    /// <param name="utf8Bytes">UTF-8 encoded TOON text.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <returns>The decoded JsonNode object.</returns>
    public static JsonNode? Decode(byte[] utf8Bytes, ToonDecodeOptions? options)
    {
        if (utf8Bytes == null)
            throw new ArgumentNullException(nameof(utf8Bytes));
        var text = Encoding.UTF8.GetString(utf8Bytes);
        return Decode(text, options ?? new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a UTF-8 byte array into the specified type with default options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="utf8Bytes">UTF-8 encoded TOON text.</param>
    public static T? Decode<T>(byte[] utf8Bytes)
    {
        return Decode<T>(utf8Bytes, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a UTF-8 byte array into the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="utf8Bytes">UTF-8 encoded TOON text.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    public static T? Decode<T>(byte[] utf8Bytes, ToonDecodeOptions? options)
    {
        if (utf8Bytes == null)
            throw new ArgumentNullException(nameof(utf8Bytes));
        var text = Encoding.UTF8.GetString(utf8Bytes);
        return Decode<T>(text, options ?? new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a stream (UTF-8) into a JsonNode with default options.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <returns>The decoded JsonNode object.</returns>
    public static JsonNode? Decode(Stream stream)
    {
        return Decode(stream, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a stream (UTF-8) into a JsonNode with custom options.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <returns>The decoded JsonNode object.</returns>
    public static JsonNode? Decode(Stream stream, ToonDecodeOptions? options)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = reader.ReadToEnd();
        return Decode(text, options ?? new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a stream (UTF-8) into the specified type with default options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="stream">The input stream to read from.</param>
    public static T? Decode<T>(Stream stream)
    {
        return Decode<T>(stream, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodes TOON data from a stream (UTF-8) into the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    public static T? Decode<T>(Stream stream, ToonDecodeOptions? options)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = reader.ReadToEnd();
        return Decode<T>(text, options ?? new ToonDecodeOptions());
    }

    #region Async Methods

    /// <summary>
    /// Asynchronously decodes a TOON-formatted string into a JsonNode with default options.
    /// </summary>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the decoded JsonNode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static Task<JsonNode?> DecodeAsync(string toonString, CancellationToken cancellationToken = default)
    {
        return DecodeAsync(toonString, new ToonDecodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously decodes a TOON-formatted string into a JsonNode with custom options.
    /// </summary>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the decoded JsonNode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString or options is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static Task<JsonNode?> DecodeAsync(string toonString, ToonDecodeOptions? options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Decode(toonString, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Asynchronously decodes a TOON-formatted string into the specified type with default options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized value.</returns>
    public static Task<T?> DecodeAsync<T>(string toonString, CancellationToken cancellationToken = default)
    {
        return DecodeAsync<T>(toonString, new ToonDecodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously decodes a TOON-formatted string into the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="toonString">The TOON-formatted string to decode.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized value.</returns>
    public static Task<T?> DecodeAsync<T>(string toonString, ToonDecodeOptions? options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Decode<T>(toonString, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Asynchronously decodes TOON data from a stream (UTF-8) into a JsonNode with default options.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the decoded JsonNode.</returns>
    public static Task<JsonNode?> DecodeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return DecodeAsync(stream, new ToonDecodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously decodes TOON data from a stream (UTF-8) into a JsonNode with custom options.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the decoded JsonNode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static async Task<JsonNode?> DecodeAsync(Stream stream, ToonDecodeOptions? options, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return Decode(text, options ?? new ToonDecodeOptions());
    }

    /// <summary>
    /// Asynchronously decodes TOON data from a stream (UTF-8) into the specified type with default options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized value.</returns>
    public static Task<T?> DecodeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return DecodeAsync<T>(stream, new ToonDecodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously decodes TOON data from a stream (UTF-8) into the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">Target type to deserialize into.</typeparam>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="options">Decoding options to customize parsing behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static async Task<T?> DecodeAsync<T>(Stream stream, ToonDecodeOptions? options, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return Decode<T>(text, options ?? new ToonDecodeOptions());
    }

    #endregion
}
