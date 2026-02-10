using SerializerFoundation;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

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

    public static unsafe string Encode(JsonElement element)
    {
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            var writeBuffer = new NonRefArrayPoolListWriteBuffer(p, buffer.Length);
            try
            {
                var writer = new ToonWriter<NonRefArrayPoolListWriteBuffer>(ref writeBuffer, Delimiter.Comma);
                Encode(ref writer, element);
                writeBuffer.Flush();

                var decoder = Encoding.UTF8.GetDecoder();

                var charCount = 0;
                var segments = writeBuffer.GetWrittenSegments();
                while (segments.TryGetNext(out var segment))
                {
                    charCount += decoder.GetCharCount(segment, flush: false);
                }
                charCount += decoder.GetCharCount([], flush: true);

                var str = string.Create(charCount, (object?)null, (_, __) => { });
                var destination = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);

                decoder.Reset();  // reusing decoder to handling carry-over
                segments.Reset(); // iterate again
                while (segments.TryGetNext(out var source))
                {
                    var written = decoder.GetChars(source, destination, flush: false);
                    destination = destination.Slice(written);
                }

                decoder.GetChars([], destination, flush: true);
                return str;
            }
            finally
            {
                writeBuffer.Dispose();
            }
        }
    }

    public static void Encode<TBufferWriter>(TBufferWriter bufferWriter, JsonElement element)
        where TBufferWriter : class, IBufferWriter<byte>
    {
        var writeBuffer = new NonRefBufferWriterWriteBuffer<TBufferWriter>(bufferWriter);
        try
        {
            var toonWriter = new ToonWriter<NonRefBufferWriterWriteBuffer<TBufferWriter>>(ref writeBuffer, Delimiter.Comma);
            Encode(ref toonWriter, element);
        }
        finally
        {
            writeBuffer.Dispose();
        }
    }

    public static void Encode<TWriteBuffer>(ref TWriteBuffer writeBuffer, JsonElement element)
        where TWriteBuffer : struct, IWriteBuffer
    {
        var toonWriter = new ToonWriter<TWriteBuffer>(ref writeBuffer, Delimiter.Comma);
        Encode(ref toonWriter, element);
    }

    public static void Encode<TWriteBuffer>(ref ToonWriter<TWriteBuffer> toonWriter, JsonElement element)
        where TWriteBuffer : struct, IWriteBuffer
    {
        WriteElement(ref toonWriter, element);
    }

    public static unsafe byte[] EncodeToUtf8Bytes(JsonElement element)
    {
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            var writeBuffer = new NonRefArrayPoolListWriteBuffer(p, buffer.Length);
            try
            {
                var toonWriter = new ToonWriter<NonRefArrayPoolListWriteBuffer>(ref writeBuffer, Delimiter.Comma);
                Encode(ref toonWriter, element);
                writeBuffer.Flush();

                return writeBuffer.ToArray();
            }
            finally
            {
                writeBuffer.Dispose();
            }
        }
    }

    public static async ValueTask EncodeAsync(Stream utf8Stream, JsonElement element, CancellationToken cancellationToken = default)
    {
        var writer = PipeWriter.Create(utf8Stream);
        var writeBuffer = new NonRefBufferWriterWriteBuffer<PipeWriter>(writer);
        try
        {
            Encode(ref writeBuffer, element);
        }
        finally
        {
            writeBuffer.Dispose();
        }
        await writer.FlushAsync(cancellationToken);
    }

    // Json(Array) to Toon(TabularArray)

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types.</remarks>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static unsafe string EncodeAsTabularArray(JsonElement array)
    {
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            var writeBuffer = new NonRefArrayPoolListWriteBuffer(p, buffer.Length);
            try
            {
                var writer = new ToonWriter<NonRefArrayPoolListWriteBuffer>(ref writeBuffer, Delimiter.Comma);
                EncodeAsTabularArray(ref writer, array);
                writeBuffer.Flush();

                var decoder = Encoding.UTF8.GetDecoder();

                var charCount = 0;
                var segments = writeBuffer.GetWrittenSegments();
                while (segments.TryGetNext(out var segment))
                {
                    charCount += decoder.GetCharCount(segment, flush: false);
                }
                charCount += decoder.GetCharCount([], flush: true);

                var str = string.Create(charCount, (object?)null, (_, __) => { });
                var destination = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);

                decoder.Reset();  // reusing decoder to handling carry-over
                segments.Reset(); // iterate again
                while (segments.TryGetNext(out var source))
                {
                    var written = decoder.GetChars(source, destination, flush: false);
                    destination = destination.Slice(written);
                }

                decoder.GetChars([], destination, flush: true);
                return str;
            }
            finally
            {
                writeBuffer.Dispose();
            }
        }
    }

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types.</remarks>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static void EncodeAsTabularArray<TBufferWriter>(TBufferWriter bufferWriter, JsonElement array)
        where TBufferWriter : class, IBufferWriter<byte>
    {
        var writeBuffer = new NonRefBufferWriterWriteBuffer<TBufferWriter>(bufferWriter);
        try
        {
            var toonWriter = new ToonWriter<NonRefBufferWriterWriteBuffer<TBufferWriter>>(ref writeBuffer, Delimiter.Comma);
            EncodeAsTabularArray(ref toonWriter, array);
        }
        finally
        {
            writeBuffer.Dispose();
        }
    }

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types.</remarks>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static void EncodeAsTabularArray<TWriteBuffer>(ref TWriteBuffer writeBuffer, JsonElement array)
        where TWriteBuffer : struct, IWriteBuffer
    {
        var toonWriter = new ToonWriter<TWriteBuffer>(ref writeBuffer, Delimiter.Comma);
        EncodeAsTabularArray(ref toonWriter, array);
    }

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types.</remarks>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static unsafe byte[] EncodeAsTabularArrayToUtf8Bytes(JsonElement array)
    {
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            var writeBuffer = new NonRefArrayPoolListWriteBuffer(p, buffer.Length);
            try
            {
                EncodeAsTabularArray(ref writeBuffer, array);
                writeBuffer.Flush();
                return writeBuffer.ToArray();
            }
            finally
            {
                writeBuffer.Dispose();
            }
        }
    }

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types.</remarks>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static async ValueTask EncodeAsTabularArrayAsync(Stream utf8Stream, JsonElement array, CancellationToken cancellationToken = default)
    {
        var writer = PipeWriter.Create(utf8Stream);
        var writeBuffer = new NonRefBufferWriterWriteBuffer<PipeWriter>(writer);
        try
        {
            EncodeAsTabularArray(ref writeBuffer, array);
        }
        finally
        {
            writeBuffer.Dispose();
        }
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Encodes a JSON array of objects as a tabular array using the specified ToonWriter.
    /// </summary>
    /// <remarks>All objects in the input array must have identical property names in the same order, and all
    /// property values must be Toon primitive types. The method writes the tabular array structure to the provided
    /// ToonWriter and flushes the writer upon completion.</remarks>
    /// <param name="toonWriter">The ToonWriter instance that receives the encoded tabular array data.</param>
    /// <param name="array">A JsonElement representing an array of objects to encode as a tabular array. Each object must have the same
    /// property names in the same order.</param>
    /// <exception cref="ArgumentException">Thrown if the provided JsonElement is not an array, if any element in the array is not an object, if objects
    /// have differing property names or counts, or if any property value is not a Toon primitive.</exception>
    public static void EncodeAsTabularArray<TWriteBuffer>(ref ToonWriter<TWriteBuffer> toonWriter, JsonElement array)
        where TWriteBuffer : struct, IWriteBuffer
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

        static void ThrowIfNotObject()
        {
            throw new ArgumentException("The provided JsonElement array does not contain object values, which are required to encode as a tabular array.");
        }
    }
}
