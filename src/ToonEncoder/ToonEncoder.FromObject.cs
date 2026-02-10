using SerializerFoundation;
using System.Buffers;
using System.Text.Json;

namespace Cysharp.AI;

partial class ToonEncoder
{
    public static string Encode<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;

        // use SerializeToDocument instand of SerializeToElement to avoid allocations
        // internally SerializeToDocument uses ParseRented(and return by Dispose).
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        return Encode(document.RootElement);
    }

    public static void Encode<TWriteBuffer, T>(ref TWriteBuffer writeBuffer, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TWriteBuffer : struct, IWriteBuffer
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        Encode(ref writeBuffer, document.RootElement);
    }

    public static async ValueTask EncodeAsync<T>(Stream utf8Stream, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default, CancellationToken cancellationToken = default)
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        await EncodeAsync(utf8Stream, document.RootElement, cancellationToken);
    }

    public static void Encode<TWriteBuffer, T>(ref ToonWriter<TWriteBuffer> toonWriter, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TWriteBuffer : struct, IWriteBuffer
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        Encode(ref toonWriter, document.RootElement);
    }

    public static byte[] EncodeToUtf8Bytes<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        return EncodeToUtf8Bytes(document.RootElement);
    }

    // AsTabularArray

    public static string EncodeAsTabularArray<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        return EncodeAsTabularArray(document.RootElement);
    }

    public static void EncodeAsTabularArray<TWriteBuffer, T>(ref TWriteBuffer writeBuffer, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TWriteBuffer : struct, IWriteBuffer
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        EncodeAsTabularArray(ref writeBuffer, document.RootElement);
    }

    public static async ValueTask EncodeAsTabularArrayAsync<T>(Stream utf8Stream, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default, CancellationToken cancellationToken = default)
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        await EncodeAsTabularArrayAsync(utf8Stream, document.RootElement, cancellationToken);
    }

    public static void EncodeAsTabularArray<TWriteBuffer, T>(ref ToonWriter<TWriteBuffer> toonWriter, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TWriteBuffer : struct, IWriteBuffer
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        EncodeAsTabularArray(ref toonWriter, document.RootElement);
    }

    public static byte[] EncodeAsTabularArrayToUtf8Bytes<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
    {
        jsonSerializerOptionsWithoutToonConverter ??= RecommendJsonSerializerOptions;
        using var document = JsonSerializer.SerializeToDocument(value, jsonSerializerOptionsWithoutToonConverter);
        return EncodeAsTabularArrayToUtf8Bytes(document.RootElement);
    }
}
