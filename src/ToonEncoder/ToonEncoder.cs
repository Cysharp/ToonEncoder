using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Cysharp.AI;

public static class ToonEncoder
{
    public static string Encode(JsonElement element)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        var toonWriter = ToonWriter.Create(bufferWriter);
        WriteElement(ref toonWriter, element);
        toonWriter.Flush();
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    public static void Encode<TBufferWriter>(TBufferWriter bufferWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>
    {
        var toonWriter = ToonWriter.Create(bufferWriter);
        WriteElement(ref toonWriter, element);
        toonWriter.Flush();
    }

    public static void Encode<TBufferWriter>(ToonWriter<TBufferWriter> toonWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>
    {
        WriteElement(ref toonWriter, element);
        toonWriter.Flush();
    }

    static void WriteElement<TBufferWriter>(ref ToonWriter<TBufferWriter> toonWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var isEmpty = !element.EnumerateObject().Any();
                if (isEmpty)
                {
                    toonWriter.WriteEmptyObject();
                    break;
                }

                toonWriter.WriteStartObject();
                foreach (var item in element.EnumerateObject())
                {
                    toonWriter.WritePropertyName(item.Name);
                    WriteElement(ref toonWriter, item.Value);
                }
                toonWriter.WriteEndObject();
                break;
            case JsonValueKind.Array:
                var enumerator = element.EnumerateArray();
                if (AllElementsArePrimitive(ref enumerator, out var length))
                {
                    enumerator.Reset();

                    toonWriter.WriteStartPrimitiveArrays(length);
                    foreach (var item in enumerator)
                    {
                        WriteElement(ref toonWriter, item);
                    }
                    toonWriter.WriteEndPrimitiveArrays();
                    break;
                }
                else
                {
                    enumerator.Reset();
                    if (AllElementsAreSamePrimitiveObjects(ref enumerator, out var fieldNames, out length))
                    {
                        enumerator.Reset();

                        toonWriter.WriteStartArraysOfObjects(length, fieldNames);
                        foreach (var item in enumerator)
                        {
                            toonWriter.WriteNextRowOfArraysOfObjects();
                            foreach (var value in item.EnumerateObject())
                            {
                                WriteElement(ref toonWriter, value.Value);
                            }
                        }
                        toonWriter.WriteEndArraysOfObjects();
                        break;
                    }
                    else
                    {
                        enumerator.Reset();

                        toonWriter.WriteStartMixedAndNonUniformArrays(element.GetArrayLength());
                        foreach (var item in enumerator)
                        {
                            toonWriter.WriteNextRowOfMixedAndNonUniformArrays();
                            WriteElement(ref toonWriter, item);
                        }
                        toonWriter.WriteEndMixedAndNonUniformArrays();
                        break;
                    }
                }
            case JsonValueKind.String:
                toonWriter.WriteString(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetUInt64(out var ulongValue))
                {
                    toonWriter.WriteNumber(ulongValue);
                }
                else if (element.TryGetInt64(out var longValue))
                {
                    toonWriter.WriteNumber(longValue);
                }
                else if (element.TryGetDecimal(out var decimalValue)) // use decimal than double for better precision
                {
                    toonWriter.WriteNumber(decimalValue);
                }
                else if (element.TryGetDouble(out var doubleValue))
                {
                    toonWriter.WriteNumber(doubleValue);
                }
                break;
            case JsonValueKind.True:
                toonWriter.WriteBoolean(true);
                break;
            case JsonValueKind.False:
                toonWriter.WriteBoolean(false);
                break;
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
            default:
                toonWriter.WriteNull();
                break;
        }
    }

    static bool AllElementsArePrimitive(ref JsonElement.ArrayEnumerator enumerator, out int length)
    {
        length = 0;
        foreach (var item in enumerator)
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.Undefined:
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return false;
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                default:
                    length++;
                    continue;
            }
        }
        return true;
    }


    static bool AllElementsAreSamePrimitiveObjects(ref JsonElement.ArrayEnumerator enumerator, out string[] fieldNames, out int length)
    {
        List<(string, ToonPrimitive)>? namesList = null;
        length = 0;
        foreach (var item in enumerator)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                fieldNames = default!;
                return false;
            }

            // first-one
            if (namesList == null)
            {
                namesList = new();
                foreach (var value in item.EnumerateObject())
                {
                    switch (value.Value.ValueKind)
                    {
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Object:
                        case JsonValueKind.Array:
                            fieldNames = default!;
                            return false;
                        case JsonValueKind.String:
                        case JsonValueKind.Number:
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                        case JsonValueKind.Null:
                        default:
                            namesList.Add((value.Name, ToToonPrimitive(value.Value.ValueKind)));
                            continue;
                    }
                }
                length++;
                continue;
            }

            var nameIndex = 0;
            var names = CollectionsMarshal.AsSpan(namesList);
            foreach (var value in item.EnumerateObject())
            {
                if (nameIndex >= names.Length)
                {
                    fieldNames = default!;
                    return false;
                }

                ref var x = ref names[nameIndex++];
                if (value.Name == x.Item1 && ToToonPrimitive(value.Value.ValueKind) == x.Item2)
                {
                    continue;
                }

                fieldNames = default!;
                return false;
            }
            length++;
        }

        fieldNames = namesList!.Select(x => x.Item1).ToArray();
        return true;
    }

    static ToonPrimitive ToToonPrimitive(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.String => ToonPrimitive.String,
            JsonValueKind.Number => ToonPrimitive.Number,
            JsonValueKind.True or JsonValueKind.False => ToonPrimitive.Boolean,
            JsonValueKind.Null or JsonValueKind.Undefined => ToonPrimitive.Null,
            _ => throw new InvalidOperationException("Not a primitive."),
        };
    }

    public enum ToonPrimitive
    {
        String, Number, Boolean, Null
    }
}

