using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkSuite;
using Perfolizer.Horology;
using System;
using System.Linq;

#if DEBUG

var bench = new EncodeBenchmark();
var a = bench.ToonEncoder_EncodeAsTabularArray_SourceGenerated();
var b = bench.ToonEncoder_Encode();
var c = bench.ToonEncoder_EncodeAsTabularArray();
var d = bench.Toon_Encode();
var e = bench.ToonSharp_Serialize();
var f = bench.ToonNet_Encode();
var g = bench.ToonDotNet_Encode();
var reference = bench.ToonOfficial_Encode();

Console.WriteLine(reference.SequenceEqual(a));
Console.WriteLine(reference.SequenceEqual(b));
Console.WriteLine(reference.SequenceEqual(c));
Console.WriteLine(reference.SequenceEqual(d));
Console.WriteLine(reference.SequenceEqual(e));
Console.WriteLine(reference.SequenceEqual(f));
Console.WriteLine(reference.SequenceEqual(g));

#else
var config = DefaultConfig.Instance
    .WithSummaryStyle(SummaryStyle.Default)
    // .WithTimeUnit(TimeUnit.Millisecond))
    .HideColumns(BenchmarkDotNet.Columns.Column.Error)
    ;

config.AddDiagnoser(MemoryDiagnoser.Default);

config.AddJob(Job.ShortRun
                 .WithToolchain(CsProjCoreToolchain.NetCoreApp10_0) // .NET 10
                 .DontEnforcePowerPlan());

var _ = BenchmarkRunner.Run(typeof(Program).Assembly, config);
#endif