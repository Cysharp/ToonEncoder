using Cysharp.AI;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Text;
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

var uu = new User(Id: 1, Name: "Alice", Role: "Admin", dt: DateTime.Now, dt2: DateTimeOffset.Now, ts: TimeSpan.FromHours(1), me: MyEnum.Fruit);
if (uu.dt == null)
{
}

var str = Encoding.UTF8.GetString(arrayBufferWriter.WrittenSpan);
Console.WriteLine(str);


[GenerateToonTabularArrayConverter]
public record User(int Id, string Name, string Role, DateTime dt, DateTimeOffset dt2, TimeSpan ts, MyEnum me);


public enum MyEnum
{
    Fruit, Orange, Apple
}
