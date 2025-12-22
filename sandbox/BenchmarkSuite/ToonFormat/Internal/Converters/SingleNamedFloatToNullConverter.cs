using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToonFormat.Internal.Converters
{
    /// <summary>
    /// Normalizes float NaN/Infinity to null when writing JSON; reading keeps default behavior.
    /// </summary>
    internal sealed class SingleNamedFloatToNullConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetSingle();

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteNumberValue(value);
        }
    }
}
