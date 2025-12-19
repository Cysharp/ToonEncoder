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
    [GenerateToonTabularArrayConverter]
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
            return Cysharp.AI.Converters.BenchmarkSuite_PersonTabularArrayConverter.EncodeAsTabularArray(data);
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
}
