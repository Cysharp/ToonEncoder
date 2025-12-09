using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("""
{
    "pairs": [["a", "b"], ["c", "d"]]
}    
""");

var toon = ToonEncoder.Encode(json);

Console.WriteLine(toon);

