namespace Cysharp.AI;

// https://toonformat.dev/guide/format-overview#arrays
partial struct ToonWriter<TWriteBuffer>
{
    public void WriteStartInlineArray(int arrayLength)
    {
        WriteRaw((byte)'[');
        FormatInt64(arrayLength);
        WriteDelimiterForArrayLength();
        WriteRaw((byte)']');

        var emitSpace = arrayLength != 0;

        if (!TryWriteKeyValueSeparator(emitSpace: emitSpace))
        {
            if (emitSpace)
            {
                WriteRaw(": "u8);
            }
            else
            {
                WriteRaw(":"u8);
            }
        }

        currentState.Push(new DepthState { Scope = WriteScope.InlineArray, Index = 0 });
    }

    public void WriteEndInlineArray()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.InlineArray) ThrowInvalidState();
        currentState.Pop();
    }

    // only allows primitive-values inside.

    public void WriteStartTabularArray(int arrayLength, IEnumerable<string> fieldNames)
    {
        WriteRaw((byte)'[');
        FormatInt64(arrayLength);
        WriteDelimiterForArrayLength();
        WriteRaw("]{"u8);

        var isFirst = true;
        foreach (var fieldName in fieldNames)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                WriteRaw((byte)delimiter);
            }

            WriteUtf16String(fieldName, QuoteScope.ObjectKey); // tabular is in key
        }

        WriteRaw("}"u8);
        if (!TryWriteKeyValueSeparator(emitSpace: false))
        {
            WriteRaw(":"u8);
        }

        currentState.Push(new DepthState { Scope = WriteScope.TabularArray, Index = 0 });
    }

    public void WriteStartTabularArray(int arrayLength, IEnumerable<ReadOnlyMemory<byte>> utf8FieldNames, bool escaped)
    {
        WriteRaw((byte)'[');
        FormatInt64(arrayLength);
        WriteDelimiterForArrayLength();
        WriteRaw("]{"u8);

        var isFirst = true;
        foreach (var fieldName in utf8FieldNames)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                WriteRaw(delimiter);
            }

            if (escaped)
            {
                WriteEscapedUtf8String(fieldName.Span, QuoteScope.ObjectKey);
            }
            else
            {
                WriteUtf8String(fieldName.Span, QuoteScope.ObjectKey);
            }
        }

        WriteRaw("}"u8);
        if (!TryWriteKeyValueSeparator(emitSpace: false))
        {
            WriteRaw(":"u8);
        }

        currentState.Push(new DepthState { Scope = WriteScope.TabularArray, Index = 0 });
    }


    public void WriteNextRowOfTabularArray()
    {
        if (currentState.Count == 0) ThrowInvalidState();
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.TabularArray) ThrowInvalidState();

        state.Index = 0; // reset index

        WriteRaw((byte)'\n');
        WriteIndent();
    }

    public void WriteEndTabularArray()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.TabularArray) ThrowInvalidState();
        currentState.Pop();
    }

    public void WriteStartNonUniformArray(int arrayLength)
    {
        WriteRaw((byte)'[');
        FormatInt64(arrayLength);
        WriteDelimiterForArrayLength();
        WriteRaw((byte)']');
        if (!TryWriteKeyValueSeparator(emitSpace: false))
        {
            WriteRaw(":"u8);
        }

        currentState.Push(new DepthState { Scope = WriteScope.NonUniformArray, Index = 0 });
    }

    public void WriteNextRowOfNonUniformArray()
    {
        if (currentState.Count == 0) ThrowInvalidState();
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.NonUniformArray) ThrowInvalidState();

        WriteRaw((byte)'\n');
        WriteIndent();
        WriteRaw("- "u8);
    }

    public void WriteEmptyNextRowOfNonUniformArray()
    {
        if (currentState.Count == 0) ThrowInvalidState();
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.NonUniformArray) ThrowInvalidState();

        WriteRaw((byte)'\n');
        WriteIndent();
        WriteRaw("-"u8);
    }

    public void WriteEndNonUniformArray()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.NonUniformArray) ThrowInvalidState();
        currentState.Pop();
    }

    void WriteDelimiter()
    {
        if (currentState.Count == 0) return;
        ref var state = ref currentState.PeekRefOrNullRef();

        if (!(state.Scope == WriteScope.InlineArray || state.Scope == WriteScope.TabularArray)) return;
        if (state.Index != 0)
        {
            WriteRaw(delimiter);
        }
        state.Index++;
    }

    void WriteDelimiterForArrayLength()
    {
        if (Delimiter == Delimiter.Comma) return;
        WriteRaw(delimiter);
    }
}
