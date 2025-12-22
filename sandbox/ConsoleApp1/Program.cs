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


var str = Cysharp.AI.Converters.SimpleClassSimpleObjectConverter.Encode(new SimpleClass
{
    Id = 1,
    Name = "Test",
    NotMyUser = new[]
    {
        new User(1, "User1", "Admin", DateTime.Now, DateTimeOffset.Now, TimeSpan.FromHours(1), MyEnum.Fruit),
        new User(2, "User2", "User", DateTime.Now, DateTimeOffset.Now, TimeSpan.FromHours(2), MyEnum.Orange),
    },
    Me = MyEnum.Apple,
    MyProperty = new[] { 1, 2, 3, 4, 5 },
    MyUser = new[]
    {
        new User(1, "User1", "Admin", DateTime.Now, DateTimeOffset.Now, TimeSpan.FromHours(1), MyEnum.Fruit),
        new User(2, "User2", "User", DateTime.Now, DateTimeOffset.Now, TimeSpan.FromHours(2), MyEnum.Orange),
    }
});

Console.WriteLine(str);



[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role, DateTime dt, DateTimeOffset dt2, TimeSpan ts, MyEnum me);


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
