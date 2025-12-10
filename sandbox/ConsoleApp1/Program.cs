using Cysharp.AI;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;


//var toon = ToonEncoder.EncodeAsTabularArray(JsonSerializer.SerializeToElement(new Person[]{
//    new(1, "Aliceだよ", 30),
//    new(2, "Bob", 25),
//    new(3, "Charlie", 35)
//}, ToonEncoder.RecommendJsonSerializerOptions));


var toon = ToonEncoder.Encode(JsonSerializer.SerializeToElement(new Person(1, "Aliceだよ", 30), ToonEncoder.RecommendJsonSerializerOptions));


Console.WriteLine(toon);

public record Person(int Id, string Name, int Age);