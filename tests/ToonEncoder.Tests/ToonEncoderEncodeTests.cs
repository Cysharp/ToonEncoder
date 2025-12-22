using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Cysharp.AI;
using TUnit.Core;

namespace ToonEncoder.Tests;

public class ToonEncoderEncodeTests
{
    const string ExpectedToon = "id: 123\nname: Ada\nactive: true";

    [Test]
    public async Task Encode_ReturnsExpectedToonString()
    {
        var element = CreateSampleElement();

        var result = Cysharp.AI.ToonEncoder.Encode(element);

        await Assert.That(result).IsEqualTo(ExpectedToon);
    }

    [Test]
    public async Task Encode_WithBufferWriter_WritesUtf8Bytes()
    {
        var element = CreateSampleElement();
        var bufferWriter = new ArrayBufferWriter<byte>();

        Cysharp.AI.ToonEncoder.Encode(ref bufferWriter, element);

        var result = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        await Assert.That(result).IsEqualTo(ExpectedToon);
    }

    [Test]
    public async Task EncodeAsync_WritesUtf8ToStream()
    {
        var element = CreateSampleElement();
        await using var stream = new MemoryStream();

        await Cysharp.AI.ToonEncoder.EncodeAsync(stream, element, CancellationToken.None);

        var result = Encoding.UTF8.GetString(stream.ToArray());
        await Assert.That(result).IsEqualTo(ExpectedToon);
    }

    [Test]
    public async Task Encode_WithToonWriter_FlushesResult()
    {
        var element = CreateSampleElement();
        var bufferWriter = new ArrayBufferWriter<byte>();
        var toonWriter = ToonWriter.Create(ref bufferWriter);

        Cysharp.AI.ToonEncoder.Encode(ref toonWriter, element);
        toonWriter.Flush();

        var result = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        await Assert.That(result).IsEqualTo(ExpectedToon);
    }

    [Test]
    public async Task EncodeToUtf8Bytes_ReturnsExpectedPayload()
    {
        var element = CreateSampleElement();

        var bytes = Cysharp.AI.ToonEncoder.EncodeToUtf8Bytes(element);
        var result = Encoding.UTF8.GetString(bytes);

        await Assert.That(result).IsEqualTo(ExpectedToon);
    }

    static JsonElement CreateSampleElement()
    {
        using var document = JsonDocument.Parse("""{"id":123,"name":"Ada","active":true}""");
        return document.RootElement.Clone();
    }
}
