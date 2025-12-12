using Cysharp.AI;
using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;


Console.OutputEncoding = Encoding.GetEncoding("utf-8");

var persons = new Person[]{
        new(1, "Alice\"だよ\"", 30),
        new(2, "Bob", 25),
        new(3, "Charlie", 35)
    };

var toon = ToonEncoder.Encode(
    JsonSerializer.SerializeToElement(persons, ToonEncoder.RecommendJsonSerializerOptions)
);

 Console.WriteLine(toon);

public record Person(int Id, string Name, int Age);

