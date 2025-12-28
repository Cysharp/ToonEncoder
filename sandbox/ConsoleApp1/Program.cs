using Cysharp.AI;

var item = new Item { Status = "OK", Users = [new(1, "Alice", "Admin"), new(2, "Bob", "User")] };

var toon = Cysharp.AI.Converters.ItemSimpleObjectConverter.Encode(item);

// Status: OK
// Users[2]{Id,Name,Role}:
//   1,Alice,Admin
//   2,Bob,User
Console.WriteLine(toon);

[GenerateToonSimpleObjectConverter]
record Item
{
    public required string Status { get; init; }
    public required User[] Users { get; init; }
}

[GenerateToonTabularArrayConverter]
record User(int Id, string Name, string Role);

[GenerateToonSimpleObjectConverter]
class SimpleClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public User[]? NotMyUser { get; set; }
    public MyEnum Me { get; set; }
    public int[]? MyProperty { get; set; }
    public User[]? MyUser { get; set; }
    // public User2 MyProperty1000 { get; set; }
}

enum MyEnum
{
    Fruit,
    Orange,
    Apple,
}

class User2
{
    public List<int>? MyProperty2 { get; set; }
}
