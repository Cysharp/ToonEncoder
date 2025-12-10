using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Cysharp.AI;

public class ToonConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
{

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // ToonEncoder.Encode(




        var element = JsonSerializer.SerializeToElement(value, options);

        
        var bufferWriter = new ArrayBufferWriter<byte>();

        

        // System.Text.Json.Serialization.JsonConverterAttribute
        // writer.WriteStringValue(


        // throw new NotImplementedException();
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
