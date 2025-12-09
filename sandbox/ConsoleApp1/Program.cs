using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("""
{
"pairs": [[], []]
}    
""");

var toon = ToonEncoder.Encode(json);


// JsonSerializer.SerializeToElement(

Console.WriteLine(toon);

