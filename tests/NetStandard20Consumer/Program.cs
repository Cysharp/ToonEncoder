using Cysharp.AI;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

#if LEGACY_RUNTIME
AssertEqual(
    ".NETStandard,Version=v2.0",
    typeof(ToonEncoder).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName,
    "The consumer must execute the package netstandard2.0 runtime asset.");
#else
AssertEqual(
    ".NETCoreApp,Version=v10.0",
    typeof(ToonEncoder).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName,
    "The consumer must execute the package net10.0 runtime asset.");
#endif

AssertEqual("text: \"quote\\\" slash\\\\ tab\\t newline\\n \\uD83D\\uDE00\"", ToonEncoder.Encode(new { text = "quote\" slash\\ tab\t newline\n 😀" }));
AssertEqual("integer: 42\nfloating: 3.14", ToonEncoder.Encode(new { integer = 42, floating = 3.14 }));
AssertEqual("status: 0\nwhen: \"2024-01-02T03:04:05Z\"\nid: 00112233-4455-6677-8899-aabbccddeeff", ToonEncoder.Encode(new { status = Status.Active, when = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff") }));
AssertEqual("[2]{id,name}:\n  1,Ada\n  2,Grace", ToonEncoder.Encode(JsonDocument.Parse("[{\"id\":1,\"name\":\"Ada\"},{\"id\":2,\"name\":\"Grace\"}]").RootElement));

var generated = Cysharp.AI.Converters.GeneratedUserTabularArrayConverter.EncodeAsTabularArray(
    [new GeneratedUser(1, "Ada"), new GeneratedUser(2, "Grace")]);
AssertEqual("[2]{Id,Name}:\n  1,Ada\n  2,Grace", generated);

#if LEGACY_RUNTIME
Console.WriteLine("Package, generator, and netstandard2.0 runtime validation passed.");
#else
Console.WriteLine("Package, generator, and net10.0 runtime validation passed.");
#endif

static void AssertEqual(string expected, string? actual, string? message = null)
{
    if (!String.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(message ?? $"Expected '{expected}', but received '{actual}'.");
    }
}

[GenerateToonTabularArrayConverter]
public record GeneratedUser(int Id, string Name);

public enum Status
{
    Active,
}
