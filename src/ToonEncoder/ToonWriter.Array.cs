namespace Cysharp.AI;

// https://toonformat.dev/guide/format-overview#arrays
partial struct ToonWriter<TBufferWriter>
{
    public void WriteStartPrimitiveArrays(int arrayLength)
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

        currentState.Push(new DepthState { Scope = WriteScope.PrimitiveArrays, Index = 0 });
    }

    public void WriteEndPrimitiveArrays()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.PrimitiveArrays) ThrowInvalidState();
        currentState.Pop();
    }

    // only allows primitive-values inside.

    public void WriteStartArraysOfObjects(int arrayLength, IEnumerable<string> fieldNames)
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
                WriteRaw((byte)Delimiter);
            }

            WriteUtf16String(fieldName, QuoteScope.ObjectKey); // tabular is in key
        }

        WriteRaw("}"u8);
        if (!TryWriteKeyValueSeparator(emitSpace: false))
        {
            WriteRaw(":"u8);
        }

        currentState.Push(new DepthState { Scope = WriteScope.ObjectArrays, Index = 0 });
    }

    public void WriteNextRowOfArraysOfObjects()
    {
        if (currentState.Count == 0) ThrowInvalidState();
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.ObjectArrays) ThrowInvalidState();

        state.Index = 0; // reset index

        WriteRaw((byte)'\n');
        WriteIndent();
    }

    public void WriteEndArraysOfObjects()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.ObjectArrays) ThrowInvalidState();
        currentState.Pop();
    }

    public void WriteStartMixedAndNonUniformArrays(int arrayLength)
    {
        WriteRaw((byte)'[');
        FormatInt64(arrayLength);
        WriteDelimiterForArrayLength();
        WriteRaw((byte)']');
        if (!TryWriteKeyValueSeparator(emitSpace: false))
        {
            WriteRaw(":"u8);
        }

        currentState.Push(new DepthState { Scope = WriteScope.MixedAndNonUniformArrays, Index = 0 });
    }

    public void WriteNextRowOfMixedAndNonUniformArrays()
    {
        if (currentState.Count == 0) ThrowInvalidState();
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.MixedAndNonUniformArrays) ThrowInvalidState();

        WriteRaw((byte)'\n');
        WriteIndent();
        WriteRaw("- "u8);
    }

    public void WriteEmptyNextRowOfMixedAndNonUniformArrays()
    {
        if (currentState.Count == 0) ThrowInvalidState();
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.MixedAndNonUniformArrays) ThrowInvalidState();

        WriteRaw((byte)'\n');
        WriteIndent();
        WriteRaw("-"u8);
    }

    public void WriteEndMixedAndNonUniformArrays()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.MixedAndNonUniformArrays) ThrowInvalidState();
        currentState.Pop();
    }

    void WriteDelimiter()
    {
        if (currentState.Count == 0) return;
        ref var state = ref currentState.PeekRefOrNullRef();

        if (!(state.Scope == WriteScope.PrimitiveArrays || state.Scope == WriteScope.ObjectArrays)) return;
        if (state.Index != 0)
        {
            WriteRaw((byte)Delimiter);
        }
        state.Index++;
    }

    void WriteDelimiterForArrayLength()
    {
        if (Delimiter == Delimiter.Comma) return;
        WriteRaw((byte)Delimiter);
    }
}