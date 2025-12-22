// https://toonformat.dev/guide/format-overview#objects
using System.Runtime.CompilerServices;

namespace Cysharp.AI;

partial struct ToonWriter<TBufferWriter>
{
    public void WriteStartObject()
    {
        TryWriteKeyValueSeparator(emitSpace: false);

        if (currentState.Count > 0)
        {
            ref var state = ref currentState.PeekRefOrNullRef();
            if (state.Scope == WriteScope.Objects)
            {
                WriteRaw((byte)'\n');
            }
        }
        currentState.Push(new DepthState { Scope = WriteScope.Objects, Index = 0 });
    }

    public void WriteEndObject()
    {
        if (currentState.Count == 0) return;
        if (currentState.PeekRefOrNullRef().Scope != WriteScope.Objects) ThrowInvalidState();
        currentState.Pop();
    }

    public void WriteEmptyObject()
    {
        TryWriteKeyValueSeparator(emitSpace: false);
    }

    public void WritePropertyName(ReadOnlySpan<char> propertyName)
    {
        if (currentState.Count == 0) ThrowInvalidState();

        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.Objects) ThrowInvalidState();

        if (state.Index != 0)
        {
            WriteRaw((byte)'\n');
        }

        ref var state2 = ref currentState.PeekTRefTwoOrNullRef();
        if (!Unsafe.IsNullRef(ref state2) && state2.Scope == WriteScope.NonUniformArray && state.Index == 0)
        {
            // Special case: no needs indent.
        }
        else
        {
            WriteIndent();
        }

        WriteUtf16String(propertyName, QuoteScope.ObjectKey);

        state.Index++;
        currentState.Push(new DepthState() { Scope = WriteScope.PropertyName, Index = 0 });
    }

    public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName)
    {
        if (currentState.Count == 0) ThrowInvalidState();

        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.Objects) ThrowInvalidState();

        if (state.Index != 0)
        {
            WriteRaw((byte)'\n');
        }

        ref var state2 = ref currentState.PeekTRefTwoOrNullRef();
        if (!Unsafe.IsNullRef(ref state2) && state2.Scope == WriteScope.NonUniformArray && state.Index == 0)
        {
            // Special case: no needs indent.
        }
        else
        {
            WriteIndent();
        }

        WriteUtf8String(utf8PropertyName, QuoteScope.ObjectKey);

        state.Index++;
        currentState.Push(new DepthState() { Scope = WriteScope.PropertyName, Index = 0 });
    }

    public void WriteEscapedPropertyName(ReadOnlySpan<byte> utf8EscapedPropertyName)
    {
        if (currentState.Count == 0) ThrowInvalidState();

        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope != WriteScope.Objects) ThrowInvalidState();

        if (state.Index != 0)
        {
            WriteRaw((byte)'\n');
        }

        ref var state2 = ref currentState.PeekTRefTwoOrNullRef();
        if (!Unsafe.IsNullRef(ref state2) && state2.Scope == WriteScope.NonUniformArray && state.Index == 0)
        {
            // Special case: no needs indent.
        }
        else
        {
            WriteIndent();
        }

        WriteEscapedUtf8String(utf8EscapedPropertyName, QuoteScope.ObjectKey);

        state.Index++;
        currentState.Push(new DepthState() { Scope = WriteScope.PropertyName, Index = 0 });
    }

    bool TryWriteKeyValueSeparator(bool emitSpace = true)
    {
        if (currentState.Count == 0) return false;
        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope == WriteScope.PropertyName)
        {
            if (emitSpace)
            {
                WriteRaw(": "u8);
            }
            else
            {
                WriteRaw(":"u8);
            }
            currentState.Pop();
            return true;
        }
        return false;
    }

    void WriteIndent()
    {
        if (currentState.Count == 0)
        {
            return;
        }

        ref var state = ref currentState.PeekRefOrNullRef();
        if (state.Scope == WriteScope.InlineArray) return;

        // get depth count
        var arrayOfObjectsDepth = 0;
        var objectsDepth = 0;
        var mixedAndNonUniformArraysDepth = 0;
        foreach (var item in currentState.AsSpan())
        {
            if (item.Scope == WriteScope.TabularArray)
            {
                arrayOfObjectsDepth++;
            }
            else if (item.Scope == WriteScope.Objects)
            {
                objectsDepth++;
            }
            else if (item.Scope == WriteScope.NonUniformArray)
            {
                mixedAndNonUniformArraysDepth++;
            }
        }

        objectsDepth = currentState.AsSpan()[0].Scope == WriteScope.Objects
            ? Math.Max(objectsDepth - 1, 0) // root object no needs indent
            : objectsDepth;

        // objectsDepth = objectsDepth; // - mixedAndNonUniformArraysDepth; // under mixed, no needs indent
        var depth = objectsDepth + arrayOfObjectsDepth + mixedAndNonUniformArraysDepth;

        switch (depth)
        {
            case 0:
                break;
            case 1:
                WriteRaw("  "u8);
                break;
            case 2:
                WriteRaw("    "u8);
                break;
            case 3:
                WriteRaw("      "u8);
                break;
            case 4:
                WriteRaw("        "u8);
                break;
            default:
                for (int i = 0; i < depth; i++)
                {
                    WriteRaw("  "u8);
                }
                break;
        }
    }
}
