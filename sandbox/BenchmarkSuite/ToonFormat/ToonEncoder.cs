#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToonFormat;
using ToonFormat.Internal.Encode;

namespace Toon.Format;

/// <summary>
/// Encodes data structures into TOON format.
/// </summary>
public static class ToonEncoder
{
    /// <summary>
    /// Encodes the specified object into TOON format with default options.
    /// </summary>
    /// <param name="data">The object to encode.</param>
    /// <returns>A TOON-formatted string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string Encode(object? data)
    {
        return Encode(data, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified value into TOON format with default options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <returns>A TOON-formatted string representation of the value.</returns>
    public static string Encode<T>(T data)
    {
        return Encode(data, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified object into TOON format with custom options.
    /// </summary>
    /// <param name="data">The object to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <returns>A TOON-formatted string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string Encode(object? data, ToonEncodeOptions? options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Normalize the input to JsonNode representation
        var normalized = Normalize.NormalizeValue(data);

        // Resolve options
        var resolvedOptions = new ResolvedEncodeOptions
        {
            Indent = options.Indent,
            Delimiter = Constants.ToDelimiterChar(options.Delimiter),
            KeyFolding = options.KeyFolding,
            FlattenDepth = options.FlattenDepth ?? int.MaxValue,
        };

        // Encode to TOON format
        return Encoders.EncodeValue(normalized, resolvedOptions);
    }

    /// <summary>
    /// Encodes the specified value into TOON format with custom options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <returns>A TOON-formatted string representation of the value.</returns>
    public static string Encode<T>(T data, ToonEncodeOptions? options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var normalized = Normalize.NormalizeValue(data);

        var resolvedOptions = new ResolvedEncodeOptions
        {
            Indent = options.Indent,
            Delimiter = Constants.ToDelimiterChar(options.Delimiter),
            KeyFolding = options.KeyFolding,
            FlattenDepth = options.FlattenDepth ?? int.MaxValue,
        };

        return Encoders.EncodeValue(normalized, resolvedOptions);
    }

    /// <summary>
    /// Encodes the specified object into UTF-8 bytes with default options.
    /// </summary>
    /// <param name="data">The object to encode.</param>
    /// <returns>UTF-8 encoded TOON bytes.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static byte[] EncodeToBytes(object? data)
    {
        return EncodeToBytes(data, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified object into UTF-8 bytes with custom options.
    /// </summary>
    /// <param name="data">The object to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <returns>UTF-8 encoded TOON bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static byte[] EncodeToBytes(object? data, ToonEncodeOptions? options)
    {
        var text = Encode(data, options);
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Encodes the specified value into UTF-8 bytes with default options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <returns>UTF-8 encoded TOON bytes.</returns>
    public static byte[] EncodeToBytes<T>(T data)
    {
        var text = Encode(data, new ToonEncodeOptions());
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Encodes the specified value into UTF-8 bytes with custom options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <returns>UTF-8 encoded TOON bytes.</returns>
    public static byte[] EncodeToBytes<T>(T data, ToonEncodeOptions? options)
    {
        var text = Encode(data, options);
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Encodes the specified object and writes UTF-8 bytes to the destination stream using default options.
    /// </summary>
    /// <param name="data">The object to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void EncodeToStream(object? data, Stream destination)
    {
        EncodeToStream(data, destination, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified object and writes UTF-8 bytes to the destination stream using custom options.
    /// </summary>
    /// <param name="data">The object to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <exception cref="ArgumentNullException">Thrown when destination or options is null.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void EncodeToStream(object? data, Stream destination, ToonEncodeOptions? options)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        var bytes = EncodeToBytes(data, options);
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Encodes the specified value and writes UTF-8 bytes to the destination stream using default options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    public static void EncodeToStream<T>(T data, Stream destination)
    {
        EncodeToStream(data, destination, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified value and writes UTF-8 bytes to the destination stream using custom options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    public static void EncodeToStream<T>(T data, Stream destination, ToonEncodeOptions? options)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        var bytes = EncodeToBytes(data, options);
        destination.Write(bytes, 0, bytes.Length);
    }

    #region Async Methods

    /// <summary>
    /// Asynchronously encodes the specified value into TOON format with default options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the TOON-formatted string.</returns>
    public static Task<string> EncodeAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        return EncodeAsync(data, new ToonEncodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously encodes the specified value into TOON format with custom options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the TOON-formatted string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public static Task<string> EncodeAsync<T>(T data, ToonEncodeOptions? options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Encode(data, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Asynchronously encodes the specified value into UTF-8 bytes with default options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the UTF-8 encoded TOON bytes.</returns>
    public static Task<byte[]> EncodeToBytesAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        return EncodeToBytesAsync(data, new ToonEncodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously encodes the specified value into UTF-8 bytes with custom options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the UTF-8 encoded TOON bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public static Task<byte[]> EncodeToBytesAsync<T>(T data, ToonEncodeOptions? options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = EncodeToBytes(data, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Asynchronously encodes the specified value and writes UTF-8 bytes to the destination stream using default options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static Task EncodeToStreamAsync<T>(T data, Stream destination, CancellationToken cancellationToken = default)
    {
        return EncodeToStreamAsync(data, destination, new ToonEncodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously encodes the specified value and writes UTF-8 bytes to the destination stream using custom options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when destination or options is null.</exception>
    public static async Task EncodeToStreamAsync<T>(T data, Stream destination, ToonEncodeOptions? options, CancellationToken cancellationToken = default)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        cancellationToken.ThrowIfCancellationRequested();
        var bytes = EncodeToBytes(data, options);
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
