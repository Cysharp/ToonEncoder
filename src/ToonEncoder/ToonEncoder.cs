using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cysharp.AI;

public static partial class ToonEncoder
{
    public static readonly JsonSerializerOptions RecommendJsonSerializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    // Json to Toon

    public static string Encode(JsonElement element)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        Encode(bufferWriter, element);
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    public static void Encode<TBufferWriter>(in TBufferWriter bufferWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>
    {
        Encode(ToonWriter.Create(bufferWriter), element);
    }

    public static async ValueTask EncodeAsync<TBufferWriter>(Stream utf8Stream, JsonElement element, CancellationToken cancellationToken)
        where TBufferWriter : IBufferWriter<byte>
    {
        var writer = PipeWriter.Create(utf8Stream);
        Encode(writer, element);
        await writer.FlushAsync(cancellationToken);
    }

    public static void Encode<TBufferWriter>(ToonWriter<TBufferWriter> toonWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>
    {
        WriteElement(ref toonWriter, element);
        toonWriter.Flush();
    }

    public static byte[] EncodeToUtf8Bytes(JsonElement element)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        Encode(bufferWriter, element);
        return bufferWriter.WrittenSpan.ToArray();
    }

    // Json(Array) to Toon(TabularArray)

    public static string EncodeAsTabularArray(JsonElement element)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        EncodeAsTabularArray(bufferWriter, element);
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    public static void EncodeAsTabularArray<TBufferWriter>(in TBufferWriter bufferWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>
    {
        EncodeAsTabularArray(ToonWriter.Create(bufferWriter), element);
    }

    public static async ValueTask EncodeAsTabularArrayAsync<TBufferWriter>(Stream utf8Stream, JsonElement element, CancellationToken cancellationToken)
        where TBufferWriter : IBufferWriter<byte>
    {
        var writer = PipeWriter.Create(utf8Stream);
        EncodeAsTabularArray(writer, element);
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array using the specified ToonWriter.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types. The method writes the tabular array structure to the provided
    /// ToonWriter and flushes the writer upon completion.</remarks>
    /// <typeparam name="TBufferWriter">The type of buffer writer used by the ToonWriter to write encoded bytes.</typeparam>
    /// <param name="toonWriter">The ToonWriter instance that receives the encoded tabular array data.</param>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static void EncodeAsTabularArray<TBufferWriter>(ToonWriter<TBufferWriter> toonWriter, JsonElement array)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("The provided JsonElement is not an array.", nameof(array));
        }

        var length = array.GetArrayLength();

        if (length == 0)
        {
            toonWriter.WriteStartInlineArray(0);
            toonWriter.WriteEndInlineArray();
            return;
        }

        var firstRow = array[0];
        if (firstRow.ValueKind != JsonValueKind.Object)
        {
            ThrowIfNotObject();
        }

        var rowPropertyCount = firstRow.GetPropertyCount();
        var names = array[0].EnumerateObject().Select(x => (ReadOnlyMemory<byte>)JsonMarshal.GetRawUtf8PropertyName(x).ToArray()).ToArray();
        toonWriter.WriteStartTabularArray(length, names, escaped: true);

        foreach (var item in array.EnumerateArray())
        {
            toonWriter.WriteNextRowOfTabularArray();

            if (item.ValueKind != JsonValueKind.Object) ThrowIfNotObject();

            var enumerateCount = 0;
            foreach (var property in item.EnumerateObject())
            {
                if (!IsToonPrimitive(property.Value.ValueKind))
                {
                    throw new ArgumentException($"The property '{property.Name}' is not a Toon primitive value, which is required to encode as a tabular array.");
                }

                if (!names[enumerateCount].Span.SequenceEqual(JsonMarshal.GetRawUtf8PropertyName(property)))
                {
                    throw new ArgumentException("All objects in the JsonElement array must have the same property names in the same order to encode as a tabular array.");
                }

                WriteElement(ref toonWriter, property.Value);
                enumerateCount++;
            }

            if (enumerateCount != rowPropertyCount)
            {
                throw new ArgumentException("All objects in the JsonElement array must have the same number of properties to encode as a tabular array.");
            }
        }
        toonWriter.WriteEndTabularArray();
        toonWriter.Flush();

        static void ThrowIfNotObject()
        {
            throw new ArgumentException("The provided JsonElement array does not contain object values, which are required to encode as a tabular array.");
        }
    }
}
