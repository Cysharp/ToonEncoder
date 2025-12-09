using Cysharp.AI.Internal;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace Cysharp.AI;

public static class ToonWriter
{
    public static ToonWriter<TBufferWriter> Create<TBufferWriter>(TBufferWriter bufferWriter)
        where TBufferWriter : IBufferWriter<byte>
    {
        return new ToonWriter<TBufferWriter>(bufferWriter);
    }

    public static ToonWriter<TBufferWriter> Create<TBufferWriter>(TBufferWriter bufferWriter, Delimiter delimiter)
        where TBufferWriter : IBufferWriter<byte>
    {
        return new ToonWriter<TBufferWriter>(bufferWriter) { Delimiter = delimiter };
    }
}

public enum Delimiter
{
    Comma = (byte)',',
    Tab = (byte)'\t',
    Pipe = (byte)'|',
}

internal enum WriteScope
{
    None,
    PropertyName,
    Objects,
    PrimitiveArrays,
    MixedAndNonUniformArrays,
    ObjectArrays,
}

internal struct DepthState
{
    // mutable field
    public WriteScope Scope;
    public int Index;

    public override string ToString()
    {
        return $"{Scope}, {Index}";
    }
}

public ref partial struct ToonWriter<TBufferWriter>(TBufferWriter bufferWriter)
    where TBufferWriter : IBufferWriter<byte>
{
    static readonly SearchValues<char> utf16NeedQuoteChars = SearchValues.Create(":\"\\{}[]()\n\r\t");
    static readonly SearchValues<char> utf16NeedQuoteCharsInScope = SearchValues.Create(",:\"\\{}[]()\n\r\t");
    static readonly SearchValues<char> utf16NeedQuoteCharsForKey = SearchValues.Create(" ,:\"\\{}[]()\n\r\t");
    static readonly SearchValues<char> utf16NeedEscapeChars = SearchValues.Create("\"\\\n\r\t");
    static readonly SearchValues<byte> utf8NeedQuoteChars = SearchValues.Create(":\"\\{}[]()\n\r\t"u8);
    static readonly SearchValues<byte> utf8NeedQuoteCharsInScope = SearchValues.Create(",:\"\\{}[]()\n\r\t"u8);
    static readonly SearchValues<byte> utf8NeedQuoteCharsForKey = SearchValues.Create(" ,:\"\\{}[]()\n\r\t"u8);
    static readonly SearchValues<byte> utf8NeedEscapeChars = SearchValues.Create("\"\\\n\r\t"u8);

    Span<byte> buffer;
    TBufferWriter bufferWriter = bufferWriter;

    int written;
    int totalWritten;
    RefStack<DepthState> currentState = new(); // in-object / in-array

    public int BytesCommitted => totalWritten;
    public int BytesPending => written;
    public Delimiter Delimiter { get; init; } = Delimiter.Comma;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBoolean(bool value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();
        WriteRaw(value ? "true"u8 : "false"u8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull()
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();
        WriteRaw("null"u8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            WriteString(value.AsSpan());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(ReadOnlySpan<char> value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        ref var state = ref currentState.PeekRefOrNullRef();
        var inScope = !Unsafe.IsNullRef(ref state) && state.Scope != WriteScope.None;
        WriteUtf16String(value, searchValues: inScope ? utf16NeedQuoteCharsInScope : utf16NeedQuoteChars);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(ReadOnlySpan<byte> utf8Value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        ref var state = ref currentState.PeekRefOrNullRef();
        var inScope = !Unsafe.IsNullRef(ref state) && state.Scope != WriteScope.None;
        WriteUtf8String(utf8Value, inScope ? utf8NeedQuoteCharsInScope : utf8NeedQuoteChars);
    }

    public void WriteNumber(int value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatInt64(value); }
    public void WriteNumber(long value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatInt64(value); }
    public void WriteNumber(uint value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatUInt64(value); }
    public void WriteNumber(ulong value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatUInt64(value); }
    public void WriteNumber(float value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatDouble(value); }
    public void WriteNumber(double value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatDouble(value); }
    public void WriteNumber(decimal value) { TryWriteKeyValueSeparator(); WriteDelimiter(); FormatDecimal(value); }

    void FormatInt64(long value)
    {
        const int MaxLength = 20; // -9223372036854775808
        EnsureBuffer(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        buffer = buffer.Slice(bytesWritten);
        written += bytesWritten;
    }

    void FormatUInt64(ulong value)
    {
        const int MaxLength = 20; // 18446744073709551615
        EnsureBuffer(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        buffer = buffer.Slice(bytesWritten);
        written += bytesWritten;
    }

    void FormatDouble(double value)
    {
        if (!double.IsFinite(value))
        {
            ThrowInvalidNumber();
        }
        const int MaxLength = 32;
        EnsureBuffer(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        buffer = buffer.Slice(bytesWritten);
        written += bytesWritten;
    }

    void FormatDecimal(decimal value)
    {
        // decimal max: -79228162514264337593543950335 (30 chars with sign and decimal point)
        const int MaxLength = 31;
        EnsureBuffer(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        buffer = buffer.Slice(bytesWritten);
        written += bytesWritten;
    }

    public void Flush()
    {
        if (written > 0)
        {
            bufferWriter.Advance(written);
            totalWritten += written;
            written = 0;
            buffer = default;
        }
    }

    void WriteUtf16String(ReadOnlySpan<char> value, SearchValues<char> searchValues)
    {
        if (NeedsQuote(value, searchValues))
        {
            WriteRaw("\""u8);
            WriteUtf16WithEscapeCore(value);
            WriteRaw("\""u8);
        }
        else
        {
            WriteUtf16WithEscapeCore(value);
        }
    }

    void WriteUtf8String(ReadOnlySpan<byte> value, SearchValues<byte> searchValues)
    {
        if (NeedsQuote(value, searchValues))
        {
            WriteRaw("\""u8);
            WriteUtf8WithEscapeCore(value);
            WriteRaw("\""u8);
        }
        else
        {
            WriteUtf8WithEscapeCore(value);
        }
    }


    // Quoting reference: https://toonformat.dev/guide/format-overview#when-strings-need-quotes

    static bool NeedsQuote(ReadOnlySpan<char> value, SearchValues<char> searchValues)
    {
        // It's empty ("")
        if (value.Length == 0) return true;

        // It equals "-" or starts with "-" followed by any character
        if (value[0] == '-') return true;

        // It has leading or trailing whitespace
        if (value[0] == ' ' || value[^1] == ' ') return true;

        // It equals true, false, or null (case-sensitive)
        if (value.Length == 4 && (value.SequenceEqual("true") || value.SequenceEqual("null"))) return true;
        if (value.Length == 5 && value.SequenceEqual("false")) return true;

        // It looks like a number (e.g., "42", "-3.14", "1e-6", or "05" with leading zeros)
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return true;

        // Contains special characters
        // It contains the relevant delimiter (the active delimiter inside an array scope, or the document delimiter elsewhere)
        if (value.ContainsAny(searchValues)) return true;

        return false;
    }

    static bool NeedsQuote(ReadOnlySpan<byte> value, SearchValues<byte> searchValues)
    {
        // It's empty ("")
        if (value.Length == 0) return true;

        // It equals "-" or starts with "-" followed by any character
        if (value[0] == (byte)'-') return true;

        // It has leading or trailing whitespace
        if (value[0] == (byte)' ' || value[^1] == (byte)' ') return true;

        // It equals true, false, or null (case-sensitive)
        if (value.Length == 4 && (value.SequenceEqual("true"u8) || value.SequenceEqual("null"u8))) return true;
        if (value.Length == 5 && value.SequenceEqual("false"u8)) return true;

        // It looks like a number (e.g., "42", "-3.14", "1e-6", or "05" with leading zeros)
        if (Utf8Parser.TryParse(value, out double _, out int consumed) && consumed == value.Length) return true;

        // Contains special characters
        // It contains the relevant delimiter (the active delimiter inside an array scope, or the document delimiter elsewhere)
        if (value.ContainsAny(searchValues)) return true;

        return false;
    }

    void WriteUtf16WithEscapeCore(ReadOnlySpan<char> value)
    {
        while (value.Length > 0)
        {
            var index = value.IndexOfAny(utf16NeedEscapeChars);
            if (index == -1)
            {
                WriteUtf16Core(value);
                return;
            }
            if (index > 0)
            {
                WriteUtf16Core(value.Slice(0, index));
            }
            WriteEscapedChar(value[index]);
            value = value.Slice(index + 1);
        }
    }

    void WriteUtf8WithEscapeCore(ReadOnlySpan<byte> value)
    {
        while (value.Length > 0)
        {
            var index = value.IndexOfAny(utf8NeedEscapeChars);
            if (index == -1)
            {
                WriteRaw(value);
                return;
            }
            if (index > 0)
            {
                WriteRaw(value.Slice(0, index));
            }
            WriteEscapedChar((char)value[index]);
            value = value.Slice(index + 1);
        }
    }

    void WriteUtf16Core(ReadOnlySpan<char> value)
    {
        while (value.Length > 0)
        {
            if (buffer.Length == 0)
            {
                EnsureBuffer(Math.Max(value.Length * 3, 256));
            }

            var status = Utf8.FromUtf16(value, buffer, out var charsRead, out var bytesWritten);

            if (status == OperationStatus.InvalidData)
            {
                ThrowInvalidUtf16();
            }

            value = value.Slice(charsRead);
            buffer = buffer.Slice(bytesWritten);
            written += bytesWritten;
        }
    }

    void WriteEscapedChar(char c)
    {
        var escaped = c switch
        {
            '"' => "\\\""u8,
            '\\' => "\\\\"u8,
            '\n' => "\\n"u8,
            '\r' => "\\r"u8,
            '\t' => "\\t"u8,
            _ => default
        };
        if (escaped.Length > 0)
        {
            WriteRaw(escaped);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteRaw(ReadOnlySpan<byte> value)
    {
        EnsureBuffer(value.Length);
        value.CopyTo(buffer);
        buffer = buffer.Slice(value.Length);
        written += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteRaw(byte value)
    {
        EnsureBuffer(1);
        buffer[0] = value;
        buffer = buffer.Slice(1);
        written += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureBuffer(int size)
    {
        if (buffer.Length < size)
        {
            GetNewBuffer(size);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void GetNewBuffer(int size)
    {
        if (written > 0)
        {
            bufferWriter.Advance(written);
            totalWritten += written;
        }
        buffer = bufferWriter.GetSpan(size);
        written = 0;
    }

    [DoesNotReturn]
    static void ThrowInvalidUtf16() => throw new InvalidOperationException("Invalid UTF-16 sequence.");

    [DoesNotReturn]
    static void ThrowInvalidNumber() => throw new ArgumentException("NaN and Infinity are not valid numbers.");

    [DoesNotReturn]
    static void ThrowInvalidState() => throw new ArgumentException("Write is not stareted write object/array.");
}
