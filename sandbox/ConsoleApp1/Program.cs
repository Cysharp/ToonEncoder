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

var arrayBufferWriter = new ArrayBufferWriter<byte>();
var toonWriter = ToonWriter.Create(ref arrayBufferWriter);

// write as NonUniform Array
toonWriter.WriteStartNonUniformArray(2);

toonWriter.WriteNextRowOfNonUniformArray();
toonWriter.WriteNumber(1);

toonWriter.WriteNextRowOfNonUniformArray();
toonWriter.WriteString(MyEnum.Orange);

toonWriter.WriteEndNonUniformArray();

toonWriter.Flush();

// toonWriter.WritePropertyName("

var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);


var array = new byte[] { 1, 2, 3 };

toonWriter.WriteStartInlineArray(19);
foreach (var item in array)
{
    toonWriter.WriteNumber(item);
}

toonWriter.WriteEndInlineArray();


[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role, DateTime dt, DateTimeOffset dt2, TimeSpan ts, MyEnum me);


[GenerateToonSimpleObjectConverter]
public class SimpleClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public MyEnum Me { get; set; }
    public int[]? MyProperty { get; set; }
    // public User[]? MyUser { get; set; }
}


public enum MyEnum
{
    Fruit, Orange, Apple
}

