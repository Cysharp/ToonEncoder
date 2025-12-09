using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("3.14");


var toon = ToonEncoder.Encode(json);

Console.WriteLine(toon);


var foo = json.TryGetDouble(out var d);


Console.WriteLine(d.ToString("G17"));

