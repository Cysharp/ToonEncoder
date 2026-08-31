using System;
using System.Buffers;
using System.Text;

namespace Cysharp.AI.Internal.Compatibility;

internal sealed class ToonSearchValues
{
#if NETSTANDARD2_0
    readonly char[] chars;
    readonly byte[] bytes;
#else
    readonly SearchValues<char> chars;
    readonly SearchValues<byte> bytes;
#endif

    public ToonSearchValues(string values)
    {
#if NETSTANDARD2_0
        chars = values.ToCharArray();
        bytes = Encoding.UTF8.GetBytes(values);
#else
        chars = SearchValues.Create(values);
        bytes = SearchValues.Create(Encoding.UTF8.GetBytes(values));
#endif
    }

    public bool ContainsAny(ReadOnlySpan<char> value)
    {
#if NETSTANDARD2_0
        foreach (var character in value)
        {
            if (Array.IndexOf(chars, character) >= 0) return true;
        }

        return false;
#else
        return value.ContainsAny(chars);
#endif
    }

    public bool ContainsAny(ReadOnlySpan<byte> value)
    {
#if NETSTANDARD2_0
        foreach (var character in value)
        {
            if (Array.IndexOf(bytes, character) >= 0) return true;
        }

        return false;
#else
        return value.ContainsAny(bytes);
#endif
    }

    public int IndexOfAny(ReadOnlySpan<char> value)
    {
#if NETSTANDARD2_0
        for (var i = 0; i < value.Length; i++)
        {
            if (Array.IndexOf(chars, value[i]) >= 0) return i;
        }

        return -1;
#else
        return value.IndexOfAny(chars);
#endif
    }

    public int IndexOfAny(ReadOnlySpan<byte> value)
    {
#if NETSTANDARD2_0
        for (var i = 0; i < value.Length; i++)
        {
            if (Array.IndexOf(bytes, value[i]) >= 0) return i;
        }

        return -1;
#else
        return value.IndexOfAny(bytes);
#endif
    }
}

internal static class ToonCompatibility
{
    public static bool IsFinite(double value)
    {
#if NETSTANDARD2_0
        return !double.IsNaN(value) && !double.IsInfinity(value);
#else
        return double.IsFinite(value);
#endif
    }
}
