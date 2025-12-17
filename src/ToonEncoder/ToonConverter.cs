using Cysharp.AI.Internal;
using System.Text.Json;

namespace Cysharp.AI;

public class ToonConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
{
    readonly JsonSerializerOptions jsonSerializerOptions = ToonEncoder.RecommendJsonSerializerOptions;

    public ToonConverter()
    {
    }

    public ToonConverter(JsonSerializerOptions JsonSerializerOptionsWithoutToonConverters)
    {
        this.jsonSerializerOptions = JsonSerializerOptionsWithoutToonConverters;
    }

    // don't use JsonSerializerOptions argument to avoid stack-overflow(recursive Toon encoding).
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions _)
    {
        var bufferWriter = new ValueArrayPoolBufferWriter<byte>();
        try
        {
            ToonEncoder.Encode(ref bufferWriter, value, jsonSerializerOptions);

            // Write as JSON string
            writer.WriteStringValue(bufferWriter.WrittenSpan);
        }
        finally
        {
            bufferWriter.Dispose();
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

    public ToonTabularArrayConverter(JsonSerializerOptions JsonSerializerOptionsWithoutToonConverters)
    {
        this.jsonSerializerOptions = JsonSerializerOptionsWithoutToonConverters;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions _)
    {
        var bufferWriter = new ValueArrayPoolBufferWriter<byte>();
        try
        {
            ToonEncoder.EncodeAsTabularArray(ref bufferWriter, value, jsonSerializerOptions);

            // Write as JSON string
            writer.WriteStringValue(bufferWriter.WrittenSpan);
        }
        finally
        {
            bufferWriter.Dispose();
        }
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Toon serialization only supports Write.");
    }
}
