using Cysharp.AI.Internal;
using SerializerFoundation;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace Cysharp.AI;

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
    InlineArray,
    TabularArray,
    NonUniformArray,
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

internal static class ToonSearchValues
{
    // for quote and escape checks

    public static readonly SearchValues<char> CommaUtf16NeedQuoteCharsInArray = SearchValues.Create(",:\"\\{}[]()\n\r\t");
    public static readonly SearchValues<char> CommaUtf16NeedQuoteCharsObjectKey = SearchValues.Create(" ,:\"\\{}[]()\n\r\t");
    public static readonly SearchValues<byte> CommaUtf8NeedQuoteCharsInArray = SearchValues.Create(",:\"\\{}[]()\n\r\t"u8);
    public static readonly SearchValues<byte> CommaUtf8NeedQuoteCharsObjectKey = SearchValues.Create(" ,:\"\\{}[]()\n\r\t"u8);

    public static readonly SearchValues<char> PipeUtf16NeedQuoteCharsInArray = SearchValues.Create("|:\"\\{}[]()\n\r\t");
    public static readonly SearchValues<char> PipeUtf16NeedQuoteCharsObjectKey = SearchValues.Create(" |:\"\\{}[]()\n\r\t");
    public static readonly SearchValues<byte> PipeUtf8NeedQuoteCharsInArray = SearchValues.Create("|:\"\\{}[]()\n\r\t"u8);
    public static readonly SearchValues<byte> PipeUtf8NeedQuoteCharsObjectKey = SearchValues.Create(" |:\"\\{}[]()\n\r\t"u8);

    public static readonly SearchValues<char> TabUtf16NeedQuoteCharsInArray = SearchValues.Create(":\"\\{}[]()\n\r\t");
    public static readonly SearchValues<char> TabUtf16NeedQuoteCharsObjectKey = SearchValues.Create(" :\"\\{}[]()\n\r\t");
    public static readonly SearchValues<byte> TabUtf8NeedQuoteCharsInArray = SearchValues.Create(":\"\\{}[]()\n\r\t"u8);
    public static readonly SearchValues<byte> TabUtf8NeedQuoteCharsObjectKey = SearchValues.Create(" :\"\\{}[]()\n\r\t"u8);

    public static readonly SearchValues<char> Utf16NeedQuoteChars = SearchValues.Create(":\"\\{}[]()\n\r\t");
    public static readonly SearchValues<byte> Utf8NeedQuoteChars = SearchValues.Create(":\"\\{}[]()\n\r\t"u8);

    public static readonly SearchValues<char> Utf16NeedEscapeChars = SearchValues.Create("\"\\\n\r\t");
    public static readonly SearchValues<byte> Utf8NeedEscapeChars = SearchValues.Create("\"\\\n\r\t"u8);
}

internal enum QuoteScope
{
    None, ObjectKey, InArray
}

public ref partial struct ToonWriter<TWriteBuffer>
    where TWriteBuffer : struct, IWriteBuffer
{
    ref TWriteBuffer writeBuffer;
    readonly byte delimiter;

    RefStack<DepthState> currentState = new(0); // in-object / in-array

    public long BytesWritten => writeBuffer.BytesWritten;
    public Delimiter Delimiter => (Delimiter)delimiter;

    public ToonWriter(ref TWriteBuffer writeBuffer)
    {
        this.writeBuffer = ref writeBuffer;
        this.delimiter = (byte)Delimiter.Comma;
    }

    public ToonWriter(ref TWriteBuffer writeBuffer, Delimiter delimiter)
    {
        this.writeBuffer = ref writeBuffer;
        this.delimiter = (byte)delimiter;
    }

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
        WriteUtf16String(value, inScope ? QuoteScope.InArray : QuoteScope.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(ReadOnlySpan<byte> utf8Value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        ref var state = ref currentState.PeekRefOrNullRef();
        var inScope = !Unsafe.IsNullRef(ref state) && state.Scope != WriteScope.None;
        WriteUtf8String(utf8Value, inScope ? QuoteScope.InArray : QuoteScope.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(Guid value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        var buffer = writeBuffer.GetSpan(36);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        writeBuffer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(DateTime value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        var buffer = writeBuffer.GetSpan(33);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten, new StandardFormat('O'));
        writeBuffer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(DateTimeOffset value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        var buffer = writeBuffer.GetSpan(33);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten, new StandardFormat('O'));
        writeBuffer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(TimeSpan value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        var buffer = writeBuffer.GetSpan(8);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten, new StandardFormat('O'));
        writeBuffer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        // currently Enum doesn't support IUtf8SpanFormattable: https://github.com/dotnet/runtime/issues/81500
        Span<char> dest = stackalloc char[256];
        int charsWritten = 0;
        while (!Enum.TryFormat(value, dest, out charsWritten))
        {
            if (dest.Length < 512)
            {
#pragma warning disable CA2014 // Do not use stackalloc in loops
                dest = stackalloc char[dest.Length * 2];
#pragma warning restore CA2014
            }
            else
            {
                dest = new char[dest.Length * 2]; // too large
            }
        }

        WriteUtf16Core(dest.Slice(0, charsWritten));
    }

    // TOON's escape character is similar as Json's one so JsonElement has already escaped string, don't need to escape.
    public void WriteEscapedString(ReadOnlySpan<byte> escapedUtf8Value)
    {
        TryWriteKeyValueSeparator();
        WriteDelimiter();

        ref var state = ref currentState.PeekRefOrNullRef();
        var inScope = !Unsafe.IsNullRef(ref state) && state.Scope != WriteScope.None;
        WriteEscapedUtf8String(escapedUtf8Value, inScope ? QuoteScope.InArray : QuoteScope.None);
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

        var buffer = writeBuffer.GetSpan(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        writeBuffer.Advance(bytesWritten);
    }

    void FormatUInt64(ulong value)
    {
        const int MaxLength = 20; // 18446744073709551615

        var buffer = writeBuffer.GetSpan(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        writeBuffer.Advance(bytesWritten);
    }

    void FormatDouble(double value)
    {
        // NaN and Infinity round to null as TOON format.
        if (!double.IsFinite(value))
        {
            WriteRaw("null"u8);
            return;
        }

        const int MaxLength = 32;

        var buffer = writeBuffer.GetSpan(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        writeBuffer.Advance(bytesWritten);
    }

    void FormatDecimal(decimal value)
    {
        // decimal max: -79228162514264337593543950335 (30 chars with sign and decimal point)
        const int MaxLength = 31;

        var buffer = writeBuffer.GetSpan(MaxLength);
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        writeBuffer.Advance(bytesWritten);
    }

    void WriteUtf16String(ReadOnlySpan<char> value, QuoteScope quoteScope)
    {
        if (NeedsQuote(value, quoteScope))
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

    void WriteUtf8String(ReadOnlySpan<byte> value, QuoteScope quoteScope)
    {
        if (NeedsQuote(value, quoteScope))
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

    void WriteEscapedUtf8String(ReadOnlySpan<byte> escapedValue, QuoteScope quoteScope)
    {
        if (NeedsQuote(escapedValue, quoteScope))
        {
            WriteRaw("\""u8);
            WriteRaw(escapedValue);
            WriteRaw("\""u8);
        }
        else
        {
            WriteRaw(escapedValue);
        }
    }

    // Quoting reference: https://toonformat.dev/guide/format-overview#when-strings-need-quotes

    bool NeedsQuote(ReadOnlySpan<char> value, QuoteScope quoteScope)
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
        var searchValues = (quoteScope, Delimiter) switch
        {
            (QuoteScope.InArray, Delimiter.Comma) => ToonSearchValues.CommaUtf16NeedQuoteCharsInArray,
            (QuoteScope.ObjectKey, Delimiter.Comma) => ToonSearchValues.CommaUtf16NeedQuoteCharsObjectKey,
            (QuoteScope.InArray, Delimiter.Pipe) => ToonSearchValues.PipeUtf16NeedQuoteCharsInArray,
            (QuoteScope.ObjectKey, Delimiter.Pipe) => ToonSearchValues.PipeUtf16NeedQuoteCharsObjectKey,
            (QuoteScope.InArray, Delimiter.Tab) => ToonSearchValues.TabUtf16NeedQuoteCharsInArray,
            (QuoteScope.ObjectKey, Delimiter.Tab) => ToonSearchValues.TabUtf16NeedQuoteCharsObjectKey,
            _ => ToonSearchValues.Utf16NeedQuoteChars,
        };

        if (value.ContainsAny(searchValues)) return true;

        return false;
    }

    bool NeedsQuote(ReadOnlySpan<byte> value, QuoteScope quoteScope)
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
        var searchValues = (quoteScope, Delimiter) switch
        {
            (QuoteScope.InArray, Delimiter.Comma) => ToonSearchValues.CommaUtf8NeedQuoteCharsInArray,
            (QuoteScope.ObjectKey, Delimiter.Comma) => ToonSearchValues.CommaUtf8NeedQuoteCharsObjectKey,
            (QuoteScope.InArray, Delimiter.Pipe) => ToonSearchValues.PipeUtf8NeedQuoteCharsInArray,
            (QuoteScope.ObjectKey, Delimiter.Pipe) => ToonSearchValues.PipeUtf8NeedQuoteCharsObjectKey,
            (QuoteScope.InArray, Delimiter.Tab) => ToonSearchValues.TabUtf8NeedQuoteCharsInArray,
            (QuoteScope.ObjectKey, Delimiter.Tab) => ToonSearchValues.TabUtf8NeedQuoteCharsObjectKey,
            _ => ToonSearchValues.Utf8NeedQuoteChars,
        };

        if (value.ContainsAny(searchValues)) return true;

        return false;
    }

    void WriteUtf16WithEscapeCore(ReadOnlySpan<char> value)
    {
        while (value.Length > 0)
        {
            var index = value.IndexOfAny(ToonSearchValues.Utf16NeedEscapeChars);
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
            var index = value.IndexOfAny(ToonSearchValues.Utf8NeedEscapeChars);
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

    void WriteUtf16Core(scoped ReadOnlySpan<char> value)
    {
        while (value.Length > 0)
        {
            var buffer = writeBuffer.GetSpan(value.Length * 3);

            var status = Utf8.FromUtf16(value, buffer, out var charsRead, out var bytesWritten);

            if (status == OperationStatus.InvalidData)
            {
                ThrowInvalidUtf16();
            }

            value = value.Slice(charsRead);
            writeBuffer.Advance(bytesWritten);
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
        var span = writeBuffer.GetSpan(value.Length);
        value.CopyTo(span);
        writeBuffer.Advance(value.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteRaw(byte value)
    {
        ref var reference = ref writeBuffer.GetReference(1);
        reference = value;
        writeBuffer.Advance(1);
    }

    [DoesNotReturn]
    static void ThrowInvalidUtf16() => throw new InvalidOperationException("Invalid UTF-16 sequence.");

    [DoesNotReturn]
    static void ThrowInvalidState() => throw new ArgumentException("Write is not stareted write object/array.");
}
