using Microsoft.CodeAnalysis;
using System;

namespace Cysharp.AI;

[Generator(LanguageNames.CSharp)]
public class ToonEncoderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitAttributes);

        var targetTypes = context.SyntaxProvider.ForAttributeWithMetadataName("Cysharp.AI.GenerateToonTabularArrayConverter",
            (node, cancellationToken) => true,
            (context, cancellationToken) => context);

        context.RegisterSourceOutput(targetTypes, EmitTabularArrayConverter);
    }

    static void EmitAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("ToonEncoder.Generator.g.cs", """
using System;

namespace Cysharp.AI
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateToonTabularArrayConverter : Attribute
    {
    }
}
""".ReplaceLineEndings());
    }

    static void EmitTabularArrayConverter(SourceProductionContext sourceProductionContext, GeneratorAttributeSyntaxContext attributeSyntaxContext)
    {

        Console.WriteLine("TODO");
    }
}