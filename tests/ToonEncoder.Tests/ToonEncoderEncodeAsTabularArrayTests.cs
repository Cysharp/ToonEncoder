using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Cysharp.AI;
using TUnit.Core;

namespace ToonEncoder.Tests;

public class ToonEncoderEncodeAsTabularArrayTests
{
    const string ExpectedTabular = "[2]{sku,qty}:\n  A1,2\n  B2,1";

    [Test]
    public async Task EncodeAsTabularArray_ReturnsExpectedString()
    {
        var element = CreateUniformArray();

        var result = Cysharp.AI.ToonEncoder.EncodeAsTabularArray(element);

        await Assert.That(result).IsEqualTo(ExpectedTabular);
    }

    [Test]
    public async Task EncodeAsTabularArray_WithBufferWriter_WritesUtf8Bytes()
    {
        var element = CreateUniformArray();
        var bufferWriter = new ArrayBufferWriter<byte>();

        Cysharp.AI.ToonEncoder.EncodeAsTabularArray(ref bufferWriter, element);

        var result = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        await Assert.That(result).IsEqualTo(ExpectedTabular);
    }

    [Test]
    public async Task EncodeAsTabularArrayAsync_WritesUtf8ToStream()
    {
        var element = CreateUniformArray();
        await using var stream = new MemoryStream();

        await Cysharp.AI.ToonEncoder.EncodeAsTabularArrayAsync(stream, element, CancellationToken.None);

        var result = Encoding.UTF8.GetString(stream.ToArray());
        await Assert.That(result).IsEqualTo(ExpectedTabular);
    }

    [Test]
    public async Task EncodeAsTabularArray_ToonWriter_ProducesTabularLayout()
    {
        var element = CreateUniformArray();
        var bufferWriter = new ArrayBufferWriter<byte>();
        var toonWriter = ToonWriter.Create(ref bufferWriter);

        Cysharp.AI.ToonEncoder.EncodeAsTabularArray(ref toonWriter, element);

        var result = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        await Assert.That(result).IsEqualTo(ExpectedTabular);
    }

    [Test]
    public async Task EncodeAsTabularArray_EmptyArray_UsesInlineArrayHeader()
    {
        var element = ParseJson("[]");
        var bufferWriter = new ArrayBufferWriter<byte>();
        var toonWriter = ToonWriter.Create(ref bufferWriter);

        Cysharp.AI.ToonEncoder.EncodeAsTabularArray(ref toonWriter, element);

        var result = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task EncodeAsTabularArray_ThrowsWhenInputIsNotArray()
    {
        var notArray = ParseJson("{\"sku\":\"A1\"}");

        var exception = ExpectArgumentException(notArray);
        await Assert.That(exception.ParamName).IsEqualTo("array");
        if (!exception.Message.StartsWith("The provided JsonElement is not an array.", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unexpected exception message.");
        }
    }

    [Test]
    public async Task EncodeAsTabularArray_ThrowsWhenElementIsNotObject()
    {
        var array = ParseJson("[1]");

        var exception = ExpectArgumentException(array);
        await Assert.That(exception.Message).IsEqualTo("The provided JsonElement array does not contain object values, which are required to encode as a tabular array.");
    }

    [Test]
    public async Task EncodeAsTabularArray_ThrowsWhenPropertyNamesDiffer()
    {
        var array = ParseJson("[{\"sku\":\"A1\",\"qty\":2},{\"sku\":\"B2\",\"count\":1}]");

        var exception = ExpectArgumentException(array);
        await Assert.That(exception.Message).IsEqualTo("All objects in the JsonElement array must have the same property names in the same order to encode as a tabular array.");
    }

    [Test]
    public async Task EncodeAsTabularArray_ThrowsWhenValueIsNotPrimitive()
    {
        var array = ParseJson("[{\"sku\":\"A1\",\"qty\":{\"nested\":1}}, {\"sku\":\"B2\",\"qty\":1}]");

        var exception = ExpectArgumentException(array);
        await Assert.That(exception.Message).IsEqualTo("The property 'qty' is not a Toon primitive value, which is required to encode as a tabular array.");
    }

    [Test]
    public async Task EncodeAsTabularArray_ThrowsWhenCountsDiffer()
    {
        var array = ParseJson("[{\"sku\":\"A1\",\"qty\":2},{\"sku\":\"B2\"}]");

        var exception = ExpectArgumentException(array);
        await Assert.That(exception.Message).IsEqualTo("All objects in the JsonElement array must have the same number of properties to encode as a tabular array.");
    }

    static JsonElement CreateUniformArray()
    {
        return ParseJson("[{\"sku\":\"A1\",\"qty\":2},{\"sku\":\"B2\",\"qty\":1}]");
    }

    static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    static ArgumentException ExpectArgumentException(JsonElement element)
    {
        try
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            var toonWriter = ToonWriter.Create(ref bufferWriter);
            Cysharp.AI.ToonEncoder.EncodeAsTabularArray(ref toonWriter, element);
        }
        catch (ArgumentException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected ArgumentException.");
    }
}
