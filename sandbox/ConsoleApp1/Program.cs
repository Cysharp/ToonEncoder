using Cysharp.AI;
using Cysharp.AI.Internal;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;


Console.OutputEncoding = Encoding.GetEncoding("utf-8");

var options = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Converters =
    {
        new ToonTabularArrayConverter<Person[]>(),
    },
};

var persons = new Person[]
{
    new Person(1, "Alice", 30),
    new Person(2, "Bob", 25),
    new Person(3, "Charlie", 35),
};


var foo = JsonSerializer.Serialize(persons, options);


Console.WriteLine(foo);

Console.WriteLine("---");

var hoge = JsonSerializer.Deserialize<string>(foo);
Console.WriteLine(hoge);



[Cysharp.AI.GenerateToonTabularArrayConverter]
public record Person(int Id, string Name, int Age);



public class PersonToonTabularArrayConverter : System.Text.Json.Serialization.JsonConverter<Person[]>
{
    static readonly ReadOnlyMemory<byte>[] utf8FieldNames = ["Id"u8.ToArray(), "Name"u8.ToArray(), "Age"u8.ToArray()];

    public static string EncodeAsTabularArray(Person[] value)
    {
        var bufferWriter = new Cysharp.AI.Internal.ValueArrayPoolBufferWriter<byte>();
        try
        {
            EncodeAsTabularArray(ref bufferWriter, value);
            return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
        }
        finally
        {
            bufferWriter.Dispose();
        }
    }

    public static byte[] EncodeAsTabularArrayToUtf8Bytes(Person[] value)
    {
        var bufferWriter = new ValueArrayPoolBufferWriter<byte>();
        try
        {
            EncodeAsTabularArray(ref bufferWriter, value);
            return bufferWriter.WrittenSpan.ToArray();
        }
        finally
        {
            bufferWriter.Dispose();
        }
    }

    public static async ValueTask EncodeAsTabularArrayAsync(Stream utf8Stream, Person[] value, CancellationToken cancellationToken = default)
    {
        var writer = PipeWriter.Create(utf8Stream);
        EncodeAsTabularArray(ref writer, value);
        await writer.FlushAsync(cancellationToken);
    }

    public static void EncodeAsTabularArray<TBufferWriter>(ref TBufferWriter bufferWriter, Person[] value)
        where TBufferWriter : IBufferWriter<byte>
    {
        var toonWriter = ToonWriter.Create(ref bufferWriter);
        EncodeAsTabularArray(ref toonWriter, value);
        toonWriter.Flush();
    }

    public static void EncodeAsTabularArray<TBufferWriter>(ref ToonWriter<TBufferWriter> toonWriter, Person[] value)
        where TBufferWriter : IBufferWriter<byte>
    {
        toonWriter.WriteStartTabularArray(value.Length, utf8FieldNames, escaped: true);

        foreach (var item in value)
        {
            toonWriter.WriteNextRowOfTabularArray();
            toonWriter.WriteNumber(item.Id);
            toonWriter.WriteString(item.Name);
            toonWriter.WriteNumber(item.Age);
        }

        toonWriter.WriteEndTabularArray();
    }

    public override void Write(Utf8JsonWriter utf8JsonWriter, Person[] value, JsonSerializerOptions options)
    {
        var bufferWriter = new Cysharp.AI.Internal.ValueArrayPoolBufferWriter<byte>();
        try
        {
            EncodeAsTabularArray(ref bufferWriter, value);
            utf8JsonWriter.WriteStringValue(bufferWriter.WrittenSpan);
        }
        finally
        {
            bufferWriter.Dispose();
        }
    }

    public override Person[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Toon serialization only supports Write.");
    }
}