using System.Runtime.CompilerServices;

namespace Cysharp.AI.Internal;

internal struct RefStack<T>
{
    T[] array;
    int size = 0;

    public int Count => size;

    [Obsolete("This constructor is for serialization only. Use RefStack(int initialSize) instead.", error: true)]
    public RefStack()
    {
    }

    public RefStack(int initialSize)
    {
        array = initialSize == 0 ? Array.Empty<T>() : new T[initialSize];
        size = 0;
    }

    public void Push(T value)
    {
        if (size == array.Length)
        {
            Array.Resize(ref array, Math.Max(4, array.Length * 2)); // I don't care if the stack is not deep enough to overflow.
        }
        array[size++] = value;
    }

    public void Pop()
    {
        size--;
    }

    public ref T PeekRefOrNullRef()
    {
        if (size == 0)
        {
            return ref Unsafe.NullRef<T>();
        }
        return ref array[size - 1];
    }

    public ref T PeekTRefTwoOrNullRef()
    {
        if (size <= 1)
        {
            return ref Unsafe.NullRef<T>();
        }
        return ref array[size - 2];
    }

    public ReadOnlySpan<T> AsSpan()
    {
        return array.AsSpan(0, size);
    }
}
