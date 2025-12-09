using System.Text.Json;

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

    public EncodeTestData[] LoadTestCases(string filePath)
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        var json = File.ReadAllText(fullPath);
        var fixture = JsonSerializer.Deserialize<ToonTestFixture>(json)!;
        return fixture.Tests
            .Select(x => new EncodeTestData(x))
            .ToArray();
    }

    [Test]
    [MethodDataSource(nameof(LoadTestCases), Arguments = ["fixtures/encode/primitives.json"])]
    public async Task TestPrimitives(EncodeTestData testData)
    {
        var toon = Cysharp.AI.ToonEncoder.Encode(testData.Fixture.Input);
        await Assert.That(toon).IsEqualTo(testData.Fixture.Expected);
    }
}
