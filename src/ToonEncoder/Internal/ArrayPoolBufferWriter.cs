using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Cysharp.AI.Internal;

internal class ArrayPoolBufferWriter<T> : IBufferWriter<T>
{
    public void Advance(int count)
    {
        throw new NotImplementedException();
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        throw new NotImplementedException();
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        throw new NotImplementedException();
    }
}
