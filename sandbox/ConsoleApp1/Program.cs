using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("""
[{ "id": 1 }, { "id": 2, "name": "Ada" }]
""");

var toon = ToonEncoder.Encode(json);


// JsonSerializer.SerializeToElement(

Console.WriteLine(toon);

