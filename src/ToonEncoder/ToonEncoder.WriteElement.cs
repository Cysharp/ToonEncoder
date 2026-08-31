using SerializerFoundation;
using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Cysharp.AI;

static partial class ToonEncoder
{
    // public
    public static bool IsToonPrimitive(JsonValueKind kind)
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

    static void WriteElement<TWriteBuffer>(ref ToonWriter<TWriteBuffer> toonWriter, JsonElement element)
        where TWriteBuffer : struct, IWriteBuffer
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
#if NETSTANDARD2_0
                        var fieldNamesArray = fieldNames.ToArray();
#else
                        var nameLookup = fieldNames.GetAlternateLookup<ReadOnlySpan<byte>>();
#endif
                        foreach (var item in enumerator)
                        {
                            toonWriter.WriteNextRowOfTabularArray();
                            foreach (var value in item.EnumerateObject())
                            {
#if NETSTANDARD2_0
                                var name = JsonMarshal.GetRawUtf8PropertyName(value).ToArray();
                                var keyIndex = Array.FindIndex(fieldNamesArray, f => SpanEquals(f.Name, name));
                                if (keyIndex >= 0 && IsToonPrimitive(value.Value.ValueKind))
                                {
                                    orderedProperties[keyIndex] = value;
                                }
                                else
                                {
                                    throw new InvalidOperationException("This should not happen.");
                                }
#else
                                if (nameLookup.TryGetValue(JsonMarshal.GetRawUtf8PropertyName(value), out var key))
                                {
                                    orderedProperties[key.Index] = value;
                                }
                                else
                                {
                                    throw new InvalidOperationException("This should not happen.");
                                }
#endif
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
#if NETSTANDARD2_0
                names = new HashSet<ArraysOfObjectsKey>();
#else
                names = new(4, comparer: ArraysOfObjectsKeyAlternateEqualityComparer.Default);
#endif
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
#if NETSTANDARD2_0
            var namesArray = names.ToArray();
#else
            var namesLookup = names.GetAlternateLookup<ReadOnlySpan<byte>>();
#endif
            foreach (var value in item.EnumerateObject())
            {
                if (nameIndex >= names.Count)
                {
                    fieldNames = default!;
                    return false;
                }

#if NETSTANDARD2_0
                if (IsToonPrimitive(value.Value.ValueKind) && TryFindKey(namesArray, JsonMarshal.GetRawUtf8PropertyName(value), out _))
#else
                if (namesLookup.TryGetValue(JsonMarshal.GetRawUtf8PropertyName(value), out var nameKey) && IsToonPrimitive(value.Value.ValueKind))
#endif
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

#if NETSTANDARD2_0
        fieldNames = names ?? new HashSet<ArraysOfObjectsKey>();
#else
        fieldNames = names ?? new(1);
#endif
        return true;
    }

    record struct ArraysOfObjectsKey(int Index, byte[] Name);

#if !NETSTANDARD2_0
    // Alternate is "Name"(UTF-8 slice)
    class ArraysOfObjectsKeyAlternateEqualityComparer : IEqualityComparer<ArraysOfObjectsKey>, IAlternateEqualityComparer<ReadOnlySpan<byte>, ArraysOfObjectsKey>
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
#endif

    static bool SpanEquals(byte[] left, ReadOnlySpan<byte> right)
    {
        return left.AsSpan().SequenceEqual(right);
    }

    static bool TryFindKey(ArraysOfObjectsKey[] keys, ReadOnlySpan<byte> name, out ArraysOfObjectsKey key)
    {
        for (int i = 0; i < keys.Length; i++)
        {
           if (keys[i].Name.AsSpan().SequenceEqual(name))
           {
               key = keys[i];
               return true;
           }
        }

        key = default;
        return false;
    }
}
