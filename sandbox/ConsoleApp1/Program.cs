using Cysharp.AI;

using System.Text.Json;

var json = JsonElement.Parse("""
{
       "items": [
         { "a": 1, "b": 2, "c": 3 },
         { "c": 30, "b": 20, "a": 10 }
       ]
     }
""");

var toon = ToonEncoder.Encode(json);


// JsonSerializer.SerializeToElement(

Console.WriteLine(toon);

