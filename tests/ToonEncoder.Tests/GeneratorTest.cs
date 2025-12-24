using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ToonEncoder.Tests;

#region Helper for Source Generator Testing

public static class CSharpGeneratorRunner
{
    static Compilation baseCompilation = default!;

    [ModuleInitializer]
    public static void InitializeCompilation()
    {
        var globalUsings = """
global using System;
global using Cysharp.AI;
""";

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location));
        //.Concat([
        //    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),                                                 // System.Console.dll
        //    MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location),                                        // System.ComponentModel.dll
        //    MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location), // System.ComponentModel.DataAnnotations
        //    MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonDocument).Assembly.Location),                           // System.Text.Json.dll
        //]);

        var compilation = CSharpCompilation.Create("generatortest",
            references: references,
            syntaxTrees: [CSharpSyntaxTree.ParseText(globalUsings, path: "GlobalUsings.cs")],
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true)); // .exe

        baseCompilation = compilation;
    }

    public static (Compilation, ImmutableArray<Diagnostic>) RunGenerator([StringSyntax("C#-test")] string source, string[]? preprocessorSymbols = null, AnalyzerConfigOptionsProvider? options = null)
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

public class VerifyHelper(string idPrefix)
{
    public async Task Ok([StringSyntax("C#-test")] string code, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        Console.WriteLine(codeExpr!);

        var (compilation, diagnostics) = CSharpGeneratorRunner.RunGenerator(code);
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

        var (compilation, diagnostics) = CSharpGeneratorRunner.RunGenerator(code);
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

        var (compilation, diagnostics) = CSharpGeneratorRunner.RunGenerator(code);
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

#endregion

public class GeneratorTest
{
    VerifyHelper verifier = new VerifyHelper("TEG");

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
