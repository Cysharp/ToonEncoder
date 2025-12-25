using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ToonEncoder.Tests;

public class CSharpGeneratorRunner
{
    Compilation baseCompilation = default!;

    public CSharpGeneratorRunner()
    {
        var globalUsings = """
global using System;
global using Cysharp.AI;
""";
        ;

        var compilation = CSharpCompilation.Create("generatortest",
            references: Basic.Reference.Assemblies.Net100.References.All,
            syntaxTrees: [CSharpSyntaxTree.ParseText(globalUsings, path: "GlobalUsings.cs")],
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true)); // .exe

        baseCompilation = compilation;
    }

    public (Compilation, ImmutableArray<Diagnostic>) RunGenerator([StringSyntax("C#-test")] string source, string[]? preprocessorSymbols = null, AnalyzerConfigOptionsProvider? options = null)
    {
        if (preprocessorSymbols == null)
        {
            preprocessorSymbols = new[] { "NET10_0_OR_GREATER" };
        }
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14, preprocessorSymbols: preprocessorSymbols); // C# 14

        var driver = CSharpGeneratorDriver.Create(new Cysharp.AI.ToonEncoderGenerator()).WithUpdatedParseOptions(parseOptions);
        if (options != null)
        {
            driver = (Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(options);
        }

        var compilation = baseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);
        return (newCompilation, diagnostics);
    }
}

public class VerifyHelper
{
    readonly string idPrefix = "TEG";

    [ClassDataSource<CSharpGeneratorRunner>(Shared = SharedType.PerTestSession)]
    public required CSharpGeneratorRunner Runner { get; init; }

    public async Task Ok([StringSyntax("C#-test")] string code, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        Console.WriteLine(codeExpr!);

        var (compilation, diagnostics) = Runner.RunGenerator(code);
        foreach (var item in diagnostics)
        {
            Console.WriteLine(item.ToString());
        }
        OutputGeneratedCode(compilation);

        await Assert.That(diagnostics.Length).IsZero();
    }

    public async Task Verify(int id, [StringSyntax("C#-test")] string code, string diagnosticsCodeSpan, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        Console.WriteLine(codeExpr!);

        var (compilation, diagnostics) = Runner.RunGenerator(code);
        foreach (var item in diagnostics)
        {
            Console.WriteLine(item.ToString());
        }
        OutputGeneratedCode(compilation);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(idPrefix + id.ToString("000"));

        var text = GetLocationText(diagnostics[0], compilation.SyntaxTrees);
        await Assert.That(text).IsEqualTo(diagnosticsCodeSpan);
    }

    public (string, string)[] Verify([StringSyntax("C#-test")] string code, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        Console.WriteLine(codeExpr!);

        var (compilation, diagnostics) = Runner.RunGenerator(code);
        OutputGeneratedCode(compilation);
        return diagnostics.Select(x => (x.Id, GetLocationText(x, compilation.SyntaxTrees))).ToArray();
    }

    string GetLocationText(Diagnostic diagnostic, IEnumerable<SyntaxTree> syntaxTrees)
    {
        var location = diagnostic.Location;

        var textSpan = location.SourceSpan;
        var sourceTree = location.SourceTree;
        if (sourceTree == null)
        {
            var lineSpan = location.GetLineSpan();
            if (lineSpan.Path == null) return "";

            sourceTree = syntaxTrees.FirstOrDefault(x => x.FilePath == lineSpan.Path);
            if (sourceTree == null) return "";
        }

        var text = sourceTree.GetText().GetSubText(textSpan).ToString();
        return text;
    }

    void OutputGeneratedCode(Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            if (!syntaxTree.FilePath.Contains("g.cs")) continue;
            Console.WriteLine(syntaxTree.ToString());
        }
    }
}
