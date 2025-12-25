namespace ToonEncoder.Tests;

[ClassDataSource<VerifyHelper>]
public class GeneratorTest(VerifyHelper verifier)
{
    [Test]
    public async Task TabularArrayGenerator()
    {
        await verifier.Ok("""
[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role);
""");

        await verifier.Verify(1, """
[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role, MyClass ng);

public class MyClass { }
""", "User");
    }

    [Test]
    public async Task SimpleObjectGenerator()
    {
        await verifier.Ok("""
[GenerateToonSimpleObjectConverter]
public class SimpleClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public MyEnum Me { get; set; }
    public int[]? MyProperty { get; set; }
    public User[]? MyUser { get; set; }
}

public record User(int Id, string Name, string Role);

public enum MyEnum
{
    Fruit, Orange, Apple
}
""");

        await verifier.Verify(2, """
[GenerateToonSimpleObjectConverter]
public class SimpleClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public MyEnum Me { get; set; }
    public User2 U2 { get; set; }
    public int[]? MyProperty { get; set; }
    public User[]? MyUser { get; set; }
}

public record User(int Id, string Name, string Role);

public class User2() { }

public enum MyEnum
{
    Fruit, Orange, Apple
}
""", "SimpleClass");



    }
}
