using SerializerFoundation;
using System.Text.Json;

namespace Cysharp.AI;

public class ToonConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
{
    readonly JsonSerializerOptions jsonSerializerOptions = ToonEncoder.RecommendJsonSerializerOptions;

    public ToonConverter()
    {
    }

    public ToonConverter(JsonSerializerOptions jsonSerializerOptionsWithoutToonConverters)
    {
        this.jsonSerializerOptions = jsonSerializerOptionsWithoutToonConverters;
    }

    // don't use JsonSerializerOptions argument to avoid stack-overflow(recursive Toon encoding).
    public override unsafe void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions _)
    {
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            var writeBuffer = new NonRefArrayPoolListWriteBuffer(p, buffer.Length);
            try
            {
                ToonEncoder.Encode(ref writeBuffer, value, jsonSerializerOptions); // use root jsonSerializerOptions
                writeBuffer.Flush();

                var segments = writeBuffer.GetWrittenSegments();
                while (segments.TryGetNext(out var span))
                {
                    writer.WriteStringValueSegment(span, isFinalSegment: false);
                }
                writer.WriteStringValueSegment((ReadOnlySpan<byte>)[], isFinalSegment: true);
            }
            finally
            {
                writeBuffer.Dispose();
            }
        }
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Toon serialization only supports Write.");
    }
}

public class ToonTabularArrayConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
{
    readonly JsonSerializerOptions jsonSerializerOptions = ToonEncoder.RecommendJsonSerializerOptions;

    public ToonTabularArrayConverter()
    {
    }

    public ToonTabularArrayConverter(JsonSerializerOptions jsonSerializerOptionsWithoutToonConverters)
    {
        this.jsonSerializerOptions = jsonSerializerOptionsWithoutToonConverters;
    }

    public override unsafe void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions _)
    {
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            var writeBuffer = new NonRefArrayPoolListWriteBuffer(p, buffer.Length);
            try
            {
                ToonEncoder.EncodeAsTabularArray(ref writeBuffer, value, jsonSerializerOptions); // use root jsonSerializerOptions
                writeBuffer.Flush();

                var segments = writeBuffer.GetWrittenSegments();
                while (segments.TryGetNext(out var span))
                {
                    writer.WriteStringValueSegment(span, isFinalSegment: false);
                }
                writer.WriteStringValueSegment((ReadOnlySpan<byte>)[], isFinalSegment: true);
            }
            finally
            {
                writeBuffer.Dispose();
            }
        }
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Toon serialization only supports Write.");
    }
}
