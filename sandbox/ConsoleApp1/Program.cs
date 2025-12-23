using Cysharp.AI;
using Cysharp.AI.Internal;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;


var item = new Item
{
    Status = "OK",
    Users = [new(1, "Alice", "Admin"), new(2, "Bob", "User")]
};

var toon = Cysharp.AI.Converters.ItemSimpleObjectConverter.Encode(item);

// Status: OK
// Users[2]{Id,Name,Role}:
//   1,Alice,Admin
//   2,Bob,User
Console.WriteLine(toon);


[GenerateToonSimpleObjectConverter]
public record Item
{
    public required string Status { get; init; }
    public required User[] Users { get; init; }
}

[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role);


[GenerateToonSimpleObjectConverter]
public class SimpleClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public User[]? NotMyUser { get; set; }
    public MyEnum Me { get; set; }
    public int[]? MyProperty { get; set; }
    public User[]? MyUser { get; set; }
    // public User2 MyProperty1000 { get; set; }
}


public enum MyEnum
{
    Fruit, Orange, Apple
}

public class User2
{
    public List<int>? MyProperty2 { get; set; }
}
