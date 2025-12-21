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
        // new ToonTabularArrayConverter<Person[]>(),
        new Cysharp.AI.Converters.PersonTabularArrayConverter(),
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

public class Dummy
{
}


