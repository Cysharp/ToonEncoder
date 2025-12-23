# ToonEncoder

[![GitHub Actions](https://github.com/Cysharp/ToonEncoder/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/ToonEncoder/actions) [![Releases](https://img.shields.io/github/release/Cysharp/ToonEncoder.svg)](https://github.com/Cysharp/ToonEncoder/releases)
[![Version](https://img.shields.io/nuget/v/ToonEncoder.svg?color=royalblue)](https://www.nuget.org/packages/ToonEncoder)

High performance [Token-Oriented Object Notation (TOON)](https://github.com/toon-format/toon) encoder for .NET. TOON is a token-efficient format designed to reduce LLM input/output costs. ToonEncoder enables natural handling of Toon in C# through integration with Microsoft.Extensions.AI and other features.

![](https://github.com/user-attachments/assets/6f797ef6-d8cf-4f40-a3c5-372f09c56979)

ToonEncoder has several options (Source Generator, TabularArray specialization, etc.), all of which are overwhelmingly memory-efficient and faster than other libraries.

I have developed several high-performance C# serializers ([MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp), [MemoryPack](https://github.com/Cysharp/MemoryPack), [Utf8Json](https://github.com/neuecc/Utf8Json), etc.). Leveraging the expertise gained from those projects, I have built a high-performance, memory-efficient serializer for the TOON format. When used appropriately, TOON has the potential to significantly reduce token consumption when interacting with LLMs.

A `JsonConverter<T>` is provided for seamless integration with [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) and [Microsoft Agent Framework](https://github.com/microsoft/agent-framework). A Source Generator is also included by default to achieve maximum performance for CSV-style tabular layouts (TabularArray) and flat objects.

* Json -> Toon conversion via JsonElement-based parsing
* JsonSerializer integration through built-in `JsonConverter<T>`
* Passes all [TOON Test Fixtures](https://github.com/toon-format/spec/tree/main/tests) (except indent and fold)
* Fully UTF-8 based internal processing with direct write pipeline to `IBufferWriter<byte>`
* Specialized methods and Source Generator for even better performance with TabularArray and root objects

This library was developed as part of [CompilerBrain](https://github.com/Cysharp/CompilerBrain), a C# Agent I am building. Since its purpose is to efficiently provide data to LLMs, this library only provides an encoder. Additionally, encoder options for changing indent size and folding are not implemented.

## Getting Started

This library is distributed via [NuGet](https://www.nuget.org/packages/ToonEncoder), minimal requirement is .NET 10.

```bash
dotnet add package ToonEncoder
```

```csharp
using Cysharp.AI;

var users = new User[]
{
    new (1, "Alice", "admin"),
    new (2, "Bob", "user"),
};

// simply encode
string toon = ToonEncoder.Encode(users);
Console.WriteLine(toon);

// If data is guaranteed uniform array, use EncodeAsTabularArray for best performance
string toon2 = ToonEncoder.EncodeAsTabularArray(users);
Console.WriteLine(toon2);

Console.WriteLine(toon == toon2); // same result

public record User(int Id, string Name, string Role);
```

TOON achieves significantly different serialization from JSON when using CSV-style tabular layouts (TabularArray). If you can guarantee that the data is serializable as TabularArray, explicitly calling `EncodeAsTabularArray` yields better performance by skipping the check for TabularArray serializability.

In such cases, applying `[GenerateToonTabularArrayConverter]` to the data type generates a more optimized encoder via Source Generator.

```csharp
// Converter is generated to Cysharp.AI.Converters.{TypeName}TabularArrayConverter
string toon3 = Cysharp.AI.Converters.UserTabularArrayConverter.EncodeAsTabularArray(users);

// If data can be uniform array, generate converter for better performance
[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role);
```

The Source Generator also supports generation for flat objects (primitives, arrays of primitive elements, and arrays of objects consisting only of primitive elements). `[GenerateToonSimpleObjectConverter]` handles scenarios like TabularArray + additional metadata.

```csharp
var item = new Item
{
    Status = "active",
    Users = [new(1, "Alice", "Admin"), new(2, "Bob", "User")]
};

var toon = Cysharp.AI.Converters.ItemSimpleObjectConverter.Encode(item);

// Status: active
// Users[2]{Id,Name,Role}:
//   1,Alice,Admin
//   2,Bob,User
Console.WriteLine(toon);

[GenerateToonSimpleObjectConverter]
public record Item
{
    public required string Status { get; init; }
    public required User[] Users { get; init; }
}
```

To use with [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai), prepare a `JsonSerializerOptions` with the appropriate type converter configured, and pass it to the options. For example, to change the return value of an `AIFunction` to TOON, configure `AIFunctionFactory` as follows:

```csharp
public IEnumerable<AIFunction> GetAIFunctions()
{
    var jsonSerializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters =
        {
            // setup generated converter
            new Cysharp.AI.Converters.CodeDiagnosticTabularArrayConverter(),
        }
    };
    jsonSerializerOptions.MakeReadOnly(true); // need MakeReadOnly(true) or setup converter to TypeInfoResolver

    var factoryOptions = new AIFunctionFactoryOptions
    {
        SerializerOptions = jsonSerializerOptions
    };

    yield return AIFunctionFactory.Create(GetDiagnostics, factoryOptions);
}

[Description("Get error diagnostics of the target project.")]
public CodeDiagnostic[] GetDiagnostics(string projectName)
{
    // ...
}

// Trigger of Source Generator
[GenerateToonTabularArrayConverter]
public class CodeDiagnostic
{
    public string Code { get; set; }
    public string Description { get; set; }
    public string FilePath { get; set; }
    public int LocationStart { get; set; }
    public int LocationLength { get; set; }
}
```

In this case, when the `CodeDiagnostic[]` count is large, there is a significant difference in token consumption between JSON and TOON, making TOON more advantageous. However, TOON has strengths and weaknesses. It is not recommended to return everything as TOON. It is important to evaluate the characteristics of your data and use JSON and TOON appropriately. Check the [When to use](#when-to-use) section for details.

## ToonEncoder

### JsonElement to Toon

`ToonEncoder.Encode` supports conversion from `JsonElement` to `string` or `byte[]`, and writing to `IBufferWriter<byte>` or `ToonWriter`.

```csharp
namespace Cysharp.AI;

public static class ToonEncoder
{
    public static string Encode(JsonElement element);

    public static void Encode<TBufferWriter>(ref TBufferWriter bufferWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>;

    public static void Encode<TBufferWriter>(ref ToonWriter<TBufferWriter> toonWriter, JsonElement element)
        where TBufferWriter : IBufferWriter<byte>;

    public static byte[] EncodeToUtf8Bytes(JsonElement element);

    public static async ValueTask EncodeAsync(Stream utf8Stream, JsonElement element, CancellationToken cancellationToken = default);
}
```

Using the `IBufferWriter<byte>` overload writes data directly in UTF-8, which yields better performance than going through `string` conversion.

If the `JsonElement` is an `array` where all elements appear in the same order and all are primitives (not Array or Object), the `EncodeAsTabularArray` method provides higher performance conversion.

```csharp
namespace Cysharp.AI;

public static class ToonEncoder
{
    public static string EncodeAsTabularArray(JsonElement array);

    public static void EncodeAsTabularArray<TBufferWriter>(ref TBufferWriter bufferWriter, JsonElement array)
        where TBufferWriter : IBufferWriter<byte>;

    public static byte[] EncodeAsTabularArrayToUtf8Bytes(JsonElement array);

    public static async ValueTask EncodeAsTabularArrayAsync(Stream utf8Stream, JsonElement array, CancellationToken cancellationToken = default);

    public static void EncodeAsTabularArray<TBufferWriter>(ref ToonWriter<TBufferWriter> toonWriter, JsonElement array)
        where TBufferWriter : IBufferWriter<byte>;
}
```

### Object to Toon

Conversion from `T value` to TOON internally uses `JsonSerializer` to first convert to `JsonElement`, then converts to TOON.

```csharp
namespace Cysharp.AI;

public static class ToonEncoder
{
    public static string Encode<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default);

    public static void Encode<TBufferWriter, T>(ref TBufferWriter bufferWriter, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TBufferWriter : IBufferWriter<byte>;
   
    public static byte[] EncodeToUtf8Bytes<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default);

    public static async ValueTask EncodeAsync<T>(Stream utf8Stream, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default, CancellationToken cancellationToken = default);

    public static void Encode<TBufferWriter, T>(ref ToonWriter<TBufferWriter> toonWriter, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TBufferWriter : IBufferWriter<byte>;

    // AsTabularArray

    public static string EncodeAsTabularArray<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default);

    public static void EncodeAsTabularArray<TBufferWriter, T>(ref TBufferWriter bufferWriter, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TBufferWriter : IBufferWriter<byte>;

    public static byte[] EncodeAsTabularArrayToUtf8Bytes<T>(T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default);

    public static async ValueTask EncodeAsTabularArrayAsync<T>(Stream utf8Stream, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default, CancellationToken cancellationToken = default);

    public static void EncodeAsTabularArray<TBufferWriter, T>(ref ToonWriter<TBufferWriter> toonWriter, T value, JsonSerializerOptions? jsonSerializerOptionsWithoutToonConverter = default)
        where TBufferWriter : IBufferWriter<byte>;
}
```

This method accepts `JsonSerializerOptions` as an argument, but do not include `ToonConverter<T>` or `ToonTabularArrayConverter<T>` in its converters (converters generated by `GenerateToonTabularArrayConverter` are acceptable). Doing so will cause recursive serialization resulting in a `StackOverflowException`. When no options are specified, [RecommendJsonSerializerOptions](#recommendjsonserializeroptions) is used by default.

### ToonWriter

ToonWriter is the most primitive API for flexibly controlling output and writing to `IArrayBufferWriter<byte>` in TOON format.

```csharp
using Cysharp.AI;
using System.Buffers;
using System.Text;

var arrayBufferWriter = new ArrayBufferWriter<byte>();

// create ToonWriter
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// write as InlineArray
toonWriter.WriteStartInlineArray(5);
toonWriter.WriteString("apple");
toonWriter.WriteString("banana");
toonWriter.WriteString("cherry");
toonWriter.WriteString("date");
toonWriter.WriteString("elderberry");
toonWriter.WriteEndInlineArray();

// Flush writes to the IBufferWriter<byte> passed by ref
toonWriter.Flush();

// [5]: apple,banana,cherry,date,elderberry
var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);
```

ToonWriter has primitive write methods (`WriteBoolean`, `WriteNumber`, `WriteString`, `WriteNull`), as well as start/end methods for objects (`WriteStartObject/WriteEndObject`) and three types of arrays (`WriteStartInlineArray/WriteEndInlineArray`, `WriteStartTabularArray/WriteEndTabularArray`, `WriteStartNonUniformArray/WriteEndNonUniformArray`).

WriteString accepts `string` as well as `ReadOnlySpan<byte>`, `ReadOnlySpan<char>`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, and `Enum`, encoding them as strings.

Note that ToonWriter does not flush automatically, so you must call `Flush()` at the end to complete the write correctly.

#### Object

In object format, wrap with `WriteStartObject`-`WriteEndObject`, and express `key:value` using `WritePropertyName` and `Write***`. Values can be nested, accepting arrays and objects in addition to primitives.

```csharp
var arrayBufferWriter = new ArrayBufferWriter<byte>();
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// write as object
toonWriter.WriteStartObject();

toonWriter.WritePropertyName("fruits");
toonWriter.WriteString("apple");

toonWriter.WritePropertyName("price");
toonWriter.WriteNumber(100);

toonWriter.WriteEndObject();

toonWriter.Flush();

// fruits: apple
// price: 100
var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);
```

Nested case:

```csharp
var arrayBufferWriter = new ArrayBufferWriter<byte>();
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// nested object/array
toonWriter.WriteStartObject();

toonWriter.WritePropertyName("grade");
toonWriter.WriteString("first");

toonWriter.WritePropertyName("ids");
toonWriter.WriteStartInlineArray(3);
toonWriter.WriteNumber(1);
toonWriter.WriteNumber(5);
toonWriter.WriteNumber(9);
toonWriter.WriteEndInlineArray();

toonWriter.WriteEndObject();

toonWriter.Flush();

// grade: first
// ids[3]: 1,5,9
var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);
```

Use `WriteEmptyObject` to output an empty object.

#### Array

TOON has three types of arrays.

##### InlineArray

One-dimensional arrays of primitives can be expressed as InlineArray. Start with `WriteStartInlineArray(length)` and close with `WriteEndInlineArray()`.

```csharp
var arrayBufferWriter = new ArrayBufferWriter<byte>();
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// write as inline-array
toonWriter.WriteStartInlineArray(3);
toonWriter.WriteNumber(10);
toonWriter.WriteNumber(20);
toonWriter.WriteNumber(30);
toonWriter.WriteEndInlineArray();

toonWriter.Flush();

// [3] 10,20,30
var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);
```

##### TabularArray

The CSV-like tabular format characteristic of TOON can be expressed as TabularArray. Prepare the header with `WriteStartTabularArray(length, names)`, then for each row call `WriteNextRowOfTabularArray()` followed by primitive write methods. Close the scope with `WriteEndTabularArray()`.

```csharp
var arrayBufferWriter = new ArrayBufferWriter<byte>();
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// write as Tabular Array
toonWriter.WriteStartTabularArray(2, ["id", "name", "role"]);

toonWriter.WriteNextRowOfTabularArray();
toonWriter.WriteNumber(1);
toonWriter.WriteString("Alice");
toonWriter.WriteString("admin");

toonWriter.WriteNextRowOfTabularArray();
toonWriter.WriteNumber(2);
toonWriter.WriteString("Bob");
toonWriter.WriteString("user");

toonWriter.WriteEndTabularArray();

toonWriter.Flush();

// [2]{id,name,role}:
//   1,Alice,admin
//   2,Bob,user
var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);
```

##### NonUniform Array

Arrays containing non-primitive elements (objects, arrays, etc.) start with `WriteStartNonUniformArray(length)`, call `WriteNextRowOfNonUniformArray` for each element, and close with `WriteEndNonUniformArray()`.

```csharp
var arrayBufferWriter = new ArrayBufferWriter<byte>();
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// write as NonUniform Array
toonWriter.WriteStartNonUniformArray(2);

toonWriter.WriteNextRowOfNonUniformArray();
toonWriter.WriteNumber(1);

toonWriter.WriteNextRowOfNonUniformArray();
toonWriter.WriteString("Alice");

toonWriter.WriteEndNonUniformArray();

toonWriter.Flush();

// [2]:
//   - 1
//   - Alice
var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);
```

### RecommendJsonSerializerOptions

The recommended JsonSerializerOptions for serializing as TOON is exposed as `RecommendJsonSerializerOptions`. The specific contents are as follows:

```csharp
namespace Cysharp.AI;

public static partial class ToonEncoder
{
    public static readonly JsonSerializerOptions RecommendJsonSerializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
}
```    

### GenerateToonTabularArrayConverter

In ToonEncoder, object encoding normally goes through JsonSerializer to first convert to JsonElement, resulting in an `Object -> Json -> Toon` conversion overhead. Only when elements can become TabularArray, using Source Generator enables direct `Object -> Toon (Tabular Array)` conversion, significantly improving performance.

The target is all public properties being `byte`, `ushort`, `uint`, `ulong`, `sbyte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `bool`, `char`, `Enum`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, and their `Nullable<T>` variants.

```csharp
[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role);
```

The generated class is output as `Cysharp.AI.Converters.{Namespace}_{Type}TabularArrayConverter : JsonConverter<T[]>`, with a static method `EncodeAsTabularArray(T[] array)` available. Note that the only supported collection type is array.

### GenerateToonSimpleObjectConverter

`[GenerateToonSimpleObjectConverter]` supports generation for flat root-only objects without nesting. The allowed property types are the same as `GenerateToonTabularArrayConverter`: `byte`, `ushort`, `uint`, `ulong`, `sbyte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `bool`, `char`, `Enum`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, and their `Nullable<T>` variants. Additionally, arrays of those types (converted as InlineArray) and arrays of objects containing only those types (converted as TabularArray) are permitted.

```csharp
[GenerateToonSimpleObjectConverter]
public record Item
{
    public required string Status { get; init; }
    public required int[] Extras { get; init; }
    public required User[] Users { get; init; }
}
```

The generated class is output as `Cysharp.AI.Converters.{Namespace}_{Type}SimpleObjectConverter : JsonConverter<T[]>`, with a static method `Encode(T value)` available.

### `ToonConverter<T>`

As generic JsonConverters, `ToonConverter<T>` and `ToonTabularArrayConverter<T>` are defined. Also, when passing Object/NonUniformArray that will not become TabularArray to JsonSerializer, you need to use these. Since internally it goes through `Object -> Json -> Toon` conversion, the constructor accepts `JsonSerializerOptions`, but do not include `ToonConverter<T>` or `ToonTabularArrayConverter<T>` in its converters.

## When to use

TOON offers interoperability with JSON and YAML-like readability, but it is not efficient for all data. The TOON official site states at the beginning:

> For deeply nested or non-uniform data, JSON may be more efficient.

For complex data, the token advantage is minimal, and LLMs may have difficulty interpreting it, so JSON is better. InlineArray also doesn't have significant advantage over JSON, so JSON is fine. Basically, TabularArray is ideal, or when more information is needed, a structure with flat properties on an object and a single TabularArray at the end will be readable for both humans and LLMs while being token-efficient. `[GenerateToonTabularArrayConverter]` and `[GenerateToonSimpleObjectConverter]` are designed specifically for such scenarios.

## License

This library is under the MIT License.
