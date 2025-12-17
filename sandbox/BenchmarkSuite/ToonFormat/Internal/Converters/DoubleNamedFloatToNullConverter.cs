using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToonFormat.Internal.Converters
{
    /// <summary>
    /// Normalizes double NaN/Infinity to null when writing JSON, keeping original numeric precision otherwise.
    /// Reading still uses default handling, no special conversion.
    /// Purpose: Consistent with TS spec (NaN/±Infinity -> null), and provides stable JsonElement for subsequent TOON encoding phase.
    /// </summary>
    internal sealed class DoubleNamedFloatToNullConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteNumberValue(value);
        }
    }
}
