using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("""
{
    "user": {}
}    
""");

var toon = ToonEncoder.Encode(json);

Console.WriteLine(toon);

