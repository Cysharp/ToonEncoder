using Cysharp.AI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace ToonEncoder.Tests;

public class ToonWriterCreateTest
{
    [Test]
    public async Task StructWriter()
    {
        var buffer = new byte[256];
        var bufferWriter = new StructBufferWriter(buffer);

        var writer = ToonWriter.Create(bufferWriter);

        writer.WriteStartPrimitiveArrays(3);
        writer.WriteNumber(1);
        writer.WriteNumber(2);
        writer.WriteNumber(3);
        writer.WriteEndPrimitiveArrays();

        writer.Flush();

        var str = Encoding.UTF8.GetString(buffer.AsSpan(0, bufferWriter.AdvanceCount));
        await Assert.That(str).IsEqualTo("[3]: 1,2,3");
    }

    struct StructBufferWriter(byte[] buffer) : IBufferWriter<byte>
    {
        byte[] buffer = buffer;

        public int AdvanceCount { get; private set; }

        public void Advance(int count)
        {
            AdvanceCount += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            return buffer;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return buffer;
        }
    }
}
