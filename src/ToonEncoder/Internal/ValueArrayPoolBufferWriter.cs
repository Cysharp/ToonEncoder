using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cysharp.AI.Internal;

internal struct ValueArrayPoolBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    const int DefaultInitialBufferSize = 256;

    T[] buffer;
    int index;

    public ValueArrayPoolBufferWriter() : this(DefaultInitialBufferSize)
    {
    }

    public ValueArrayPoolBufferWriter(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        index = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory => buffer.AsMemory(0, index);
    public ReadOnlySpan<T> WrittenSpan => buffer.AsSpan(0, index);

    public int WrittenCount => index;
    public int Capacity => buffer.Length;
    public int FreeCapacity => buffer.Length - index;

    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (index > buffer.Length - count)
        {
            ThrowInvalidOperationException_AdvancedTooFar();
        }
        index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return buffer.AsMemory(index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return buffer.AsSpan(index);
    }

    public void Dispose()
    {
        if (buffer.Length > 0)
        {
            ArrayPool<T>.Shared.Return(buffer);
            buffer = [];
        }
        index = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckAndResizeBuffer(int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > FreeCapacity)
        {
            int currentLength = buffer.Length;

            int growBy = Math.Max(sizeHint, currentLength);

            if (currentLength == 0)
            {
                growBy = Math.Max(growBy, DefaultInitialBufferSize);
            }

            int newSize = currentLength + growBy;

            if ((uint)newSize > int.MaxValue)
            {
                uint needed = (uint)(currentLength - FreeCapacity + sizeHint);
                if (needed > Array.MaxLength)
                {
                    ThrowOutOfMemoryException(needed);
                }

                newSize = Array.MaxLength;
            }

            var newBuffer = ArrayPool<T>.Shared.Rent(newSize);
            buffer.AsSpan(0, index).CopyTo(newBuffer);

            var toReturn = buffer;
            buffer = newBuffer;
            ArrayPool<T>.Shared.Return(toReturn);
        }
    }

    [DoesNotReturn]
    static void ThrowInvalidOperationException_AdvancedTooFar()
    {
        throw new InvalidOperationException("Cannot advance past the end of the buffer.");
    }

    [DoesNotReturn]
    static void ThrowOutOfMemoryException(uint capacity)
    {
        throw new OutOfMemoryException();
    }
}