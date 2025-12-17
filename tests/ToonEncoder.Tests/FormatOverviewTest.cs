using Cysharp.AI;
using System.Buffers;
using System.Text;

namespace ToonEncoder.Tests;

// https://toonformat.dev/guide/format-overview.html
public class FormatOverviewTest
{
    delegate void WriteBody(ref ToonWriter<ArrayBufferWriter<byte>> writer);

    // helper
    static string Encode(WriteBody write, Delimiter delimiter = Delimiter.Comma)
    {
        var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
        var toonWriter = ToonWriter.Create(ref bufferWriter, delimiter);
        write(ref toonWriter);
        toonWriter.Flush();
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan).Replace("\n", Environment.NewLine);
    }

    [Test]
    public async Task SimpleObjects()
    {
        var str = Encode((ref writer) =>
        {
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteNumber(123);

            writer.WritePropertyName("name");
            writer.WriteString("Ada");

            writer.WritePropertyName("active");
            writer.WriteBoolean(true);

            writer.WriteEndObject();
        });

        await Assert.That(str).IsEqualTo("""
id: 123
name: Ada
active: true
""");
    }

    [Test]
    public async Task NestedObjects()
    {
        var str = Encode((ref writer) =>
        {
            writer.WriteStartObject();

            writer.WritePropertyName("user");
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteNumber(123);

            writer.WritePropertyName("name");
            writer.WriteString("Ada");

            writer.WriteEndObject();

            writer.WriteEndObject();
        });

        await Assert.That(str).IsEqualTo("""
user:
  id: 123
  name: Ada
""");
    }

    [Test]
    public async Task PrimitiveArrays()
    {
        var str = Encode((ref writer) =>
        {
            writer.WriteStartObject();

            writer.WritePropertyName("tags");
            writer.WriteStartInlineArray(3);

            writer.WriteString("admin");
            writer.WriteString("ops");
            writer.WriteString("dev");

            writer.WriteEndInlineArray();

            writer.WriteEndObject();
        });

        await Assert.That(str).IsEqualTo("""
tags[3]: admin,ops,dev
""");
    }

    [Test]
    public async Task ArraysOfObjects()
    {
        var basic = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");

            writer.WriteStartTabularArray(2, ["sku", "qty", "price"]);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("A1");
            writer.WriteNumber(2);
            writer.WriteNumber(9.99);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("B2");
            writer.WriteNumber(1);
            writer.WriteNumber(14.5);

            writer.WriteEndTabularArray();

            writer.WriteEndObject();
        });

        await Assert.That(basic).IsEqualTo("""
items[2]{sku,qty,price}:
  A1,2,9.99
  B2,1,14.5
""");
        var whiteSpaces = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("users");
            writer.WriteStartTabularArray(2, ["id", "name", "role"]);

            writer.WriteNextRowOfTabularArray();
            writer.WriteNumber(1);
            writer.WriteString("Alice Admin");
            writer.WriteString("admin");

            writer.WriteNextRowOfTabularArray();
            writer.WriteNumber(2);
            writer.WriteString("Bob Smith");
            writer.WriteString("user");

            writer.WriteEndTabularArray();
            writer.WriteEndObject();
        });


        await Assert.That(whiteSpaces).IsEqualTo("""
users[2]{id,name,role}:
  1,Alice Admin,admin
  2,Bob Smith,user
""");

    }

    [Test]
    public async Task MixedAndNonUniform()
    {
        var basic = Encode((ref writer) =>
       {
           writer.WriteStartObject();
           writer.WritePropertyName("items");

           writer.WriteStartNonUniformArray(3);

           writer.WriteNextRowOfNonUniformArray();
           writer.WriteNumber(1);

           writer.WriteNextRowOfNonUniformArray();
           writer.WriteStartObject();
           writer.WritePropertyName("a");
           writer.WriteNumber(1);
           writer.WriteEndObject();

           writer.WriteNextRowOfNonUniformArray();
           writer.WriteString("text");

           writer.WriteEndNonUniformArray();

           writer.WriteEndObject();
       });

        await Assert.That(basic).IsEqualTo("""
items[3]:
  - 1
  - a: 1
  - text
""");
    }

    [Test]
    public async Task ObjectsAsListItems()
    {
        var basic = Encode((ref writer) =>
       {
           writer.WriteStartObject();
           writer.WritePropertyName("items");

           writer.WriteStartNonUniformArray(2);

           writer.WriteNextRowOfNonUniformArray();
           writer.WriteStartObject();
           writer.WritePropertyName("id");
           writer.WriteNumber(1);
           writer.WritePropertyName("name");
           writer.WriteString("First");
           writer.WriteEndObject();

           writer.WriteNextRowOfNonUniformArray();
           writer.WriteStartObject();
           writer.WritePropertyName("id");
           writer.WriteNumber(2);
           writer.WritePropertyName("name");
           writer.WriteString("Second");
           writer.WritePropertyName("extra");
           writer.WriteBoolean(true);
           writer.WriteEndObject();

           writer.WriteEndNonUniformArray();

           writer.WriteEndObject();
       });

        await Assert.That(basic).IsEqualTo("""
items[2]:
  - id: 1
    name: First
  - id: 2
    name: Second
    extra: true
""");
    }

    [Test]
    public async Task ObjectsAsListItemsWithTabluarArray()
    {
        var basic = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");

            writer.WriteStartNonUniformArray(1);

            writer.WriteNextRowOfNonUniformArray();

            writer.WriteStartObject();
            writer.WritePropertyName("users");

            writer.WriteStartTabularArray(2, ["id", "name"]);

            writer.WriteNextRowOfTabularArray();
            writer.WriteNumber(1);
            writer.WriteString("Ada");

            writer.WriteNextRowOfTabularArray();
            writer.WriteNumber(2);
            writer.WriteString("Bob");

            writer.WriteEndTabularArray();

            writer.WritePropertyName("status");
            writer.WriteString("active");

            writer.WriteEndObject();

            writer.WriteEndNonUniformArray();

            writer.WriteEndObject();
        });

        await Assert.That(basic).IsEqualTo("""
items[1]:
  - users[2]{id,name}:
      1,Ada
      2,Bob
    status: active
""");
    }


    [Test]
    public async Task ArraysOfArrays()
    {
        var basic = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("pairs");

            writer.WriteStartNonUniformArray(2);

            writer.WriteNextRowOfNonUniformArray();
            writer.WriteStartInlineArray(2);
            writer.WriteNumber(1);
            writer.WriteNumber(2);
            writer.WriteEndInlineArray();

            writer.WriteNextRowOfNonUniformArray();
            writer.WriteStartInlineArray(2);
            writer.WriteNumber(3);
            writer.WriteNumber(4);
            writer.WriteEndInlineArray();

            writer.WriteEndNonUniformArray();

            writer.WriteEndObject();
        });

        await Assert.That(basic).IsEqualTo("""
pairs[2]:
  - [2]: 1,2
  - [2]: 3,4
""");
    }

    [Test]
    public async Task EmptyArrays()
    {
        var basic = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");
            writer.WriteStartInlineArray(0);
            writer.WriteEndInlineArray();
            writer.WriteEndObject();
        });
        await Assert.That(basic).IsEqualTo("""
items[0]:
""");
    }

    [Test]
    public async Task DelimiterOptions()
    {
        var comma = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");

            writer.WriteStartTabularArray(2, ["sku", "name", "qty", "price"]);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("A1");
            writer.WriteString("Widget");
            writer.WriteNumber(2);
            writer.WriteNumber(9.99);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("B2");
            writer.WriteString("Gadget");
            writer.WriteNumber(1);
            writer.WriteNumber(14.5);

            writer.WriteEndTabularArray();

            writer.WriteEndObject();
        });

        await Assert.That(comma).IsEqualTo("""
items[2]{sku,name,qty,price}:
  A1,Widget,2,9.99
  B2,Gadget,1,14.5
""");

        var tab = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");

            writer.WriteStartTabularArray(2, ["sku", "name", "qty", "price"]);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("A1");
            writer.WriteString("Widget");
            writer.WriteNumber(2);
            writer.WriteNumber(9.99);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("B2");
            writer.WriteString("Gadget");
            writer.WriteNumber(1);
            writer.WriteNumber(14.5);

            writer.WriteEndTabularArray();

            writer.WriteEndObject();
        }, Delimiter.Tab);

        await Assert.That(tab).IsEqualTo("""
items[2	]{sku	name	qty	price}:
  A1	Widget	2	9.99
  B2	Gadget	1	14.5
""");

        var pipe = Encode((ref writer) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");

            writer.WriteStartTabularArray(2, ["sku", "name", "qty", "price"]);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("A1");
            writer.WriteString("Widget");
            writer.WriteNumber(2);
            writer.WriteNumber(9.99);

            writer.WriteNextRowOfTabularArray();
            writer.WriteString("B2");
            writer.WriteString("Gadget");
            writer.WriteNumber(1);
            writer.WriteNumber(14.5);

            writer.WriteEndTabularArray();

            writer.WriteEndObject();
        }, Delimiter.Pipe);

        await Assert.That(pipe).IsEqualTo("""
items[2|]{sku|name|qty|price}:
  A1|Widget|2|9.99
  B2|Gadget|1|14.5
""");
    }
}
