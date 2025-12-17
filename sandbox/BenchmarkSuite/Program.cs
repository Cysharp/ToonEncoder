using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using Perfolizer.Horology;


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