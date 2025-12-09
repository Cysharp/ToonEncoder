using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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

                        toonWriter.WriteStartArraysOfObjects(length, fieldNames.Select(x => x.Name));
                        var orderedProperties = new JsonProperty[fieldNames.Count];
                        var nameLookup = fieldNames.GetAlternateLookup<string>();
                        foreach (var item in enumerator)
                        {
                            toonWriter.WriteNextRowOfArraysOfObjects();
                            foreach (var value in item.EnumerateObject())
                            {
                                if (nameLookup.TryGetValue(value.Name, out var key))
                                {
                                    orderedProperties[key.Index] = value;
                                }
                                else
                                {
                                    throw new InvalidOperationException("This should not happen.");
                                }
                            }

                            foreach (var jsonProperty in orderedProperties)
                            {
                                WriteElement(ref toonWriter, jsonProperty.Value);
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
                            if (item.ValueKind == JsonValueKind.Object && !item.EnumerateObject().Any())
                            {
                                toonWriter.WriteEmptyNextRowOfMixedAndNonUniformArrays();
                            }
                            else
                            {
                                toonWriter.WriteNextRowOfMixedAndNonUniformArrays();
                                WriteElement(ref toonWriter, item);
                            }
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

    static bool AllElementsAreSamePrimitiveObjects(ref JsonElement.ArrayEnumerator enumerator, out HashSet<ArraysOfObjectsKey> fieldNames, out int length)
    {
        HashSet<ArraysOfObjectsKey>? names = null;
        length = 0;
        foreach (var item in enumerator)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                fieldNames = default!;
                return false;
            }

            // first-one
            if (names == null)
            {
                names = new(4, comparer: ArraysOfObjectsKeyAlternateEqualityComparer.Default);
                var findIndex = 0;
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
                            names.Add(new(findIndex++, value.Name, ToToonPrimitive(value.Value.ValueKind)));
                            continue;
                    }
                }
                length++;
                continue;
            }

            var nameIndex = 0;
            var namesLookup = names.GetAlternateLookup<string>();
            foreach (var value in item.EnumerateObject())
            {
                if (nameIndex >= names.Count)
                {
                    fieldNames = default!;
                    return false;
                }

                if (namesLookup.TryGetValue(value.Name, out var nameKey) && nameKey.PrimitiveKind == ToToonPrimitive(value.Value.ValueKind))
                {
                    nameIndex++;
                    continue;
                }

                fieldNames = default!;
                return false;
            }

            if (nameIndex != names.Count)
            {
                fieldNames = default!;
                return false;
            }

            length++;
        }

        fieldNames = names ?? new(1);
        return true;
    }

    static ToonPrimitive ToToonPrimitive(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.String => ToonPrimitive.String,
            JsonValueKind.Null or JsonValueKind.Undefined => ToonPrimitive.String, // null is primitive of string
            JsonValueKind.Number => ToonPrimitive.Number,
            JsonValueKind.True or JsonValueKind.False => ToonPrimitive.Boolean,
            _ => ToonPrimitive.NotPrimitive,
        };
    }

    public enum ToonPrimitive
    {
        NotPrimitive/* object or array */, String, Number, Boolean, Null
    }

    public record struct ArraysOfObjectsKey(int Index, string Name, ToonPrimitive PrimitiveKind);

    // Alternate is "Name"
    public class ArraysOfObjectsKeyAlternateEqualityComparer : IEqualityComparer<ArraysOfObjectsKey>, IAlternateEqualityComparer<string, ArraysOfObjectsKey>
    {
        public static readonly ArraysOfObjectsKeyAlternateEqualityComparer Default = new();

        // IEqualityComparer

        public bool Equals(ArraysOfObjectsKey x, ArraysOfObjectsKey y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(ArraysOfObjectsKey obj)
        {
            return obj.Name.GetHashCode();
        }

        // IAlternateEqualityComparer

        public ArraysOfObjectsKey Create(string name)
        {
            return new ArraysOfObjectsKey(0, name, ToonPrimitive.Null); // Index and PrimitiveKind are dummy here
        }

        public bool Equals(string name, ArraysOfObjectsKey other)
        {
            return name == other.Name;
        }

        public int GetHashCode(string name)
        {
            return name.GetHashCode();
        }
    }
}

