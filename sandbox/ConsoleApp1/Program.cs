using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("""
{
    "item": "null"
}
""");

var json2 = JsonSerializer.SerializeToElement(DateTime.Now);
Console.WriteLine(json2);

var toon = ToonEncoder.Encode(json);


// JsonSerializer.SerializeToElement(

Console.WriteLine(toon);

