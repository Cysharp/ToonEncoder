using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Cysharp.AI;
using Cysharp.AI.Internal;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkSuite
{
    public record Person(int Id, string Name, int Age);

    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class EncodeBenchmark
    {
        Person[] data;

        public EncodeBenchmark()
        {
            data = Enumerable.Range(0, 100)
                .Select(x => new Person(x, $"Person {x}", x * 2))
                .ToArray();
        }

        // mine

        [Benchmark]
        public string ToonEncoder_Encode()
        {
            return global::Cysharp.AI.ToonEncoder.Encode(data);
        }

        [Benchmark]
        public string ToonEncoder_EncodeAsTabularArray()
        {
            return global::Cysharp.AI.ToonEncoder.EncodeAsTabularArray(data);
        }

        [Benchmark]
        public string ToonEncoder_EncodeAsTabularArray_SourceGenerated()
        {
            return PersonToonTabularArrayConverter.EncodeAsTabularArray(data);
        }

        // https://github.com/StefH/Toon.NET
        [Benchmark]
        public string Toon_Encode()
        {
            return global::Toon.ToonEncoder.Encode(data);
        }

        // https://github.com/0xZunia/ToonSharp
        [Benchmark]
        public string ToonSharp_Serialize()
        {
            return global::ToonSharp.ToonSerializer.Serialize(data);
        }

        // https://github.com/Nicola898989/ToonNet
        [Benchmark]
        public string ToonNet_Encode()
        {
            return global::ToonNetSerializer.ToonNet.Encode(data);
        }

        // https://github.com/CharlesHunt/ToonDotNet
        [Benchmark]
        public string ToonDotNet_Encode()
        {
            return global::ToonFormat.Toon.Encode(data);
        }

        // Official impl: https://github.com/toon-format/toon-dotnet
        [Benchmark]
        public string ToonOfficial_Encode()
        {
            return global::Toon.Format.ToonEncoder.Encode(data);
        }
    }

    // TODO: test generation, finally need to remove.
    public class PersonToonTabularArrayConverter : System.Text.Json.Serialization.JsonConverter<Person[]>
    {
        static readonly ReadOnlyMemory<byte>[] utf8FieldNames = ["Id"u8.ToArray(), "Name"u8.ToArray(), "Age"u8.ToArray()];

        public static string EncodeAsTabularArray(Person[] value)
        {
            var bufferWriter = new Cysharp.AI.Internal.ValueArrayPoolBufferWriter<byte>();
            try
            {
                EncodeAsTabularArray(ref bufferWriter, value);
                return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
            }
            finally
            {
                bufferWriter.Dispose();
            }
        }

        public static byte[] EncodeAsTabularArrayToUtf8Bytes(Person[] value)
        {
            var bufferWriter = new ValueArrayPoolBufferWriter<byte>();
            try
            {
                EncodeAsTabularArray(ref bufferWriter, value);
                return bufferWriter.WrittenSpan.ToArray();
            }
            finally
            {
                bufferWriter.Dispose();
            }
        }

        public static async ValueTask EncodeAsTabularArrayAsync(Stream utf8Stream, Person[] value, CancellationToken cancellationToken = default)
        {
            var writer = PipeWriter.Create(utf8Stream);
            EncodeAsTabularArray(ref writer, value);
            await writer.FlushAsync(cancellationToken);
        }

        public static void EncodeAsTabularArray<TBufferWriter>(ref TBufferWriter bufferWriter, Person[] value)
            where TBufferWriter : IBufferWriter<byte>
        {
            var toonWriter = ToonWriter.Create(ref bufferWriter);
            EncodeAsTabularArray(ref toonWriter, value);
            toonWriter.Flush();
        }

        public static void EncodeAsTabularArray<TBufferWriter>(ref ToonWriter<TBufferWriter> toonWriter, Person[] value)
            where TBufferWriter : IBufferWriter<byte>
        {
            toonWriter.WriteStartTabularArray(value.Length, utf8FieldNames, escaped: true);

            foreach (var item in value)
            {
                toonWriter.WriteNextRowOfTabularArray();
                toonWriter.WriteNumber(item.Id);
                toonWriter.WriteString(item.Name);
                toonWriter.WriteNumber(item.Age);
            }

            toonWriter.WriteEndTabularArray();
        }

        public override void Write(Utf8JsonWriter utf8JsonWriter, Person[] value, JsonSerializerOptions options)
        {
            var bufferWriter = new Cysharp.AI.Internal.ValueArrayPoolBufferWriter<byte>();
            try
            {
                EncodeAsTabularArray(ref bufferWriter, value);
                utf8JsonWriter.WriteStringValue(bufferWriter.WrittenSpan);
            }
            finally
            {
                bufferWriter.Dispose();
            }
        }

        public override Person[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Toon serialization only supports Write.");
        }
    }
}
