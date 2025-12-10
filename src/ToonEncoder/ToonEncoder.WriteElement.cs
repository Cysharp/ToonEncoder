using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Cysharp.AI;

static partial class ToonEncoder
{
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
                    // item.Name allocates a new string instance for each call.
                    // So use GetRawUtf8PropertyName to avoid allocation.
                    var rawName = JsonMarshal.GetRawUtf8PropertyName(item);
                    toonWriter.WriteEscapedPropertyName(rawName); // already escaped in JSON
                    WriteElement(ref toonWriter, item.Value);
                }
                toonWriter.WriteEndObject();
                break;
            case JsonValueKind.Array:
                var enumerator = element.EnumerateArray();
                if (AllElementsArePrimitive(ref enumerator, out var length))
                {
                    enumerator.Reset();

                    toonWriter.WriteStartInlineArray(length);
                    foreach (var item in enumerator)
                    {
                        WriteElement(ref toonWriter, item);
                    }
                    toonWriter.WriteEndInlineArray();
                    break;
                }
                else
                {
                    enumerator.Reset();
                    if (AllObjectsAreSameNameAndPrimitive(ref enumerator, out var fieldNames, out length))
                    {
                        enumerator.Reset();

                        toonWriter.WriteStartTabularArray(length, fieldNames.Select(x => (ReadOnlyMemory<byte>)x.Name.AsMemory()), escaped: true);
                        var orderedProperties = new JsonProperty[fieldNames.Count];
                        var nameLookup = fieldNames.GetAlternateLookup<ReadOnlySpan<byte>>();
                        foreach (var item in enumerator)
                        {
                            toonWriter.WriteNextRowOfTabularArray();
                            foreach (var value in item.EnumerateObject())
                            {
                                if (nameLookup.TryGetValue(JsonMarshal.GetRawUtf8PropertyName(value), out var key))
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
                        toonWriter.WriteEndTabularArray();
                        break;
                    }
                    else
                    {
                        enumerator.Reset();

                        toonWriter.WriteStartNonUniformArray(element.GetArrayLength());
                        foreach (var item in enumerator)
                        {
                            if (item.ValueKind == JsonValueKind.Object && !item.EnumerateObject().Any())
                            {
                                toonWriter.WriteEmptyNextRowOfNonUniformArray();
                            }
                            else
                            {
                                toonWriter.WriteNextRowOfNonUniformArray();
                                WriteElement(ref toonWriter, item);
                            }
                        }
                        toonWriter.WriteEndNonUniformArray();
                        break;
                    }
                }
            case JsonValueKind.String:
                // element.GetString() allocates new array so use GetRawUtf8Value to avoid allocation.
                var utf8Value = JsonMarshal.GetRawUtf8Value(element);
                utf8Value = utf8Value.Slice(1, utf8Value.Length - 2); // GetRawUtf8Value is includeQuotes: true so trim quotes
                toonWriter.WriteEscapedString(utf8Value); // already escaped in JSON
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

    static bool AllObjectsAreSameNameAndPrimitive(ref JsonElement.ArrayEnumerator enumerator, out HashSet<ArraysOfObjectsKey> fieldNames, out int length)
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
                    if (!IsToonPrimitive(value.Value.ValueKind))
                    {
                        fieldNames = default!;
                        return false;
                    }

                    names.Add(new(findIndex++, JsonMarshal.GetRawUtf8PropertyName(value).ToArray()));
                }
                length++;
                continue;
            }

            var nameIndex = 0;
            var namesLookup = names.GetAlternateLookup<ReadOnlySpan<byte>>();
            foreach (var value in item.EnumerateObject())
            {
                if (nameIndex >= names.Count)
                {
                    fieldNames = default!;
                    return false;
                }

                if (namesLookup.TryGetValue(JsonMarshal.GetRawUtf8PropertyName(value), out var nameKey) && IsToonPrimitive(value.Value.ValueKind))
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

    static bool IsToonPrimitive(JsonValueKind kind)
    {
        switch (kind)
        {
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
            case JsonValueKind.Object:
            case JsonValueKind.Array:
            default:
                return false;
        }
    }

    public record struct ArraysOfObjectsKey(int Index, byte[] Name);

    // Alternate is "Name"
    public class ArraysOfObjectsKeyAlternateEqualityComparer : IEqualityComparer<ArraysOfObjectsKey>, IAlternateEqualityComparer<ReadOnlySpan<byte>, ArraysOfObjectsKey>
    {
        public static readonly ArraysOfObjectsKeyAlternateEqualityComparer Default = new();

        // IEqualityComparer

        public bool Equals(ArraysOfObjectsKey x, ArraysOfObjectsKey y)
        {
            return x.Name.SequenceEqual(y.Name);
        }

        public int GetHashCode(ArraysOfObjectsKey obj)
        {
            return GetHashCode(obj.Name);
        }

        // IAlternateEqualityComparer

        public ArraysOfObjectsKey Create(ReadOnlySpan<byte> alternate)
        {
            throw new NotSupportedException();
        }

        public bool Equals(ReadOnlySpan<byte> alternate, ArraysOfObjectsKey other)
        {
            return alternate.SequenceEqual(other.Name);
        }

        public int GetHashCode(ReadOnlySpan<byte> alternate)
        {
            // System.IO.Hashing package, cast to int is safe for hashing
            return unchecked((int)XxHash3.HashToUInt64(alternate));
        }
    }
}
