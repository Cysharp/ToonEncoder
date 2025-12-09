using Cysharp.AI;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using TUnit.Core;

namespace ToonEncoder.Tests;

// https://github.com/toon-format/spec/tree/main/tests

public class FixtureTest
{
    public record EncodeTestData(TestCase Fixture)
    {
        public override string ToString()
        {
            return Fixture.Name;
        }
    }

    public async Task<EncodeTestData[]> LoadTestCases(string filePath)
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        var json = await File.ReadAllTextAsync(fullPath);
        var fixture = JsonSerializer.Deserialize<ToonTestFixture>(json)!;
        return fixture.Tests
            .Select(x => new EncodeTestData(x))
            .ToArray();
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/primitives.json"])]
    public async Task Primitives(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/objects.json"])]
    public async Task Objects(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/arrays-primitive.json"])]
    public async Task ArraysPrimitive(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/arrays-tabular.json"])]
    public async Task ArraysTabular(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/arrays-objects.json"])]
    public async Task ArraysObjects(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/arrays-nested.json"])]
    public async Task ArraysNested(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/whitespace.json"])]
    public async Task Whitespace(EncodeTestData testData)
    {
        if (testData.Fixture.Name == "respects custom indent size option")
        {
            Skip.Test("configure indent size is not supported.");
        }

        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [Skip("Key folding is not supported.")]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/key-folding.json"])]
    public async Task KeyFolding(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/delimiters.json"])]
    public async Task Delimiters(EncodeTestData testData)
    {
        var delimiterString = testData.Fixture.Options!.Delimiter![0];
        var delimiter = (Delimiter)(byte)new Rune(delimiterString).Value;

        var bufferWriter = new ArrayBufferWriter<byte>();
        var toonWriter = ToonWriter.Create(bufferWriter, delimiter);

        Cysharp.AI.ToonEncoder.Encode(toonWriter, testData.Fixture.Input);

        var toon = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }
}
