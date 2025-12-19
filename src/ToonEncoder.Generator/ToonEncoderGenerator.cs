using Microsoft.CodeAnalysis;

namespace Cysharp.AI;

[Generator(LanguageNames.CSharp)]
public class ToonEncoderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitAttributes);

        var targetTypes = context.SyntaxProvider.ForAttributeWithMetadataName("Cysharp.AI.GenerateToonTabularArrayConverter",
            (node, cancellationToken) => true,
            (context, cancellationToken) =>
            {
                if (context.TargetSymbol is ITypeSymbol typeSymbol)
                {
                    return new TabularArrayInfo(typeSymbol);
                }
                else
                {
                    return null;
                }
            })
            .Where(x => x != null);

        context.RegisterSourceOutput(targetTypes, EmitTabularArrayConverter!);
    }

    static void EmitAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("GenerateToonTabularArrayConverter.g.cs", """
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

    static void EmitTabularArrayConverter(SourceProductionContext sourceProductionContext, TabularArrayInfo tabularArrayInfo)
    {
        //sourceProductionContext.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
        //    "TEG001",
        //    "Generating Toon Tabular Array Converter",
        //    $"Generating Toon Tabular Array Converter for {tabularArrayInfo.ElementFullName}",
        //    "ToonEncoderGenerator",
        //    DiagnosticSeverity.Info,
        //    isEnabledByDefault: true), Location.None));

        var arrayType = $"{tabularArrayInfo.ElementFullName}[]";
        var utf8FieldNames = string.Join(", ", tabularArrayInfo.PropertyNames.Select(n => $"\"{n}\"u8.ToArray()"));
        var encodeRow = string.Join("\n", tabularArrayInfo.PropertyNames.Select((name, index) =>
        {
            var kind = tabularArrayInfo.PropertyKinds[index];
            var str = kind switch
            {
                ToonPrimitiveKind.Boolean => $"toonWriter.WriteBoolean(item.{name});",
                ToonPrimitiveKind.String => $"toonWriter.WriteString(item.{name});",
                ToonPrimitiveKind.Number => $"toonWriter.WriteNumber(item.{name});",
                ToonPrimitiveKind.DateTime => $"toonWriter.WriteString(item.{name});",
                ToonPrimitiveKind.Guid => $"toonWriter.WriteString(item.{name});",
                ToonPrimitiveKind.Enum => $"toonWriter.WriteString(item.{name});",
                ToonPrimitiveKind.NullableBoolean => $"if (item.{name} == null) {{ toonWriter.WriteNull(); }} else {{ toonWriter.WriteBoolean(item.{name}); }}",
                ToonPrimitiveKind.NullableNumber => $"if (item.{name} == null) {{ toonWriter.WriteNull(); }} else {{ toonWriter.WriteNumber(item.{name}); }}",
                ToonPrimitiveKind.NullableDateTime => $"if (item.{name} == null) {{ toonWriter.WriteNull(); }} else {{ toonWriter.WriteString(item.{name}); }}",
                ToonPrimitiveKind.NullableGuid => $"if (item.{name} == null) {{ toonWriter.WriteNull(); }} else {{ toonWriter.WriteString(item.{name}); }}",
                ToonPrimitiveKind.NullableEnum => $"if (item.{name} == null) {{ toonWriter.WriteNull(); }} else {{ toonWriter.WriteString(item.{name}); }}",
                _ => throw new NotSupportedException($"Unsupported property type for Toon serialization: {kind}"),
            };
            return "                "/* indent */ + str;
        }));

        var source = $$"""
using Cysharp.AI.Internal;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.AI.Converters
{
    public class {{tabularArrayInfo.ConverterName}} : JsonConverter<{{arrayType}}>
    {
        static readonly ReadOnlyMemory<byte>[] utf8FieldNames = [{{utf8FieldNames}}];

        public static string EncodeAsTabularArray({{arrayType}} value)
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

        public static byte[] EncodeAsTabularArrayToUtf8Bytes({{arrayType}} value)
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

        public static async ValueTask EncodeAsTabularArrayAsync(Stream utf8Stream, {{arrayType}} value, CancellationToken cancellationToken = default)
        {
            var writer = PipeWriter.Create(utf8Stream);
            EncodeAsTabularArray(ref writer, value);
            await writer.FlushAsync(cancellationToken);
        }

        public static void EncodeAsTabularArray<TBufferWriter>(ref TBufferWriter bufferWriter, {{arrayType}} value)
            where TBufferWriter : IBufferWriter<byte>
        {
            var toonWriter = ToonWriter.Create(ref bufferWriter);
            EncodeAsTabularArray(ref toonWriter, value);
            toonWriter.Flush();
        }

        public static void EncodeAsTabularArray<TBufferWriter>(ref ToonWriter<TBufferWriter> toonWriter, {{arrayType}} value)
            where TBufferWriter : IBufferWriter<byte>
        {
            toonWriter.WriteStartTabularArray(value.Length, utf8FieldNames, escaped: true);

            foreach (var item in value)
            {
                toonWriter.WriteNextRowOfTabularArray();
{{encodeRow}}
            }

            toonWriter.WriteEndTabularArray();
        }

        public override void Write(Utf8JsonWriter utf8JsonWriter, {{arrayType}} value, JsonSerializerOptions options)
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

        public override {{arrayType}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Toon serialization only supports Write.");
        }
    }
}
""";

        sourceProductionContext.AddSource($"{tabularArrayInfo.ConverterName}.g.cs", source.ReplaceLineEndings());
    }
}

public record TabularArrayInfo
{
    public string ElementFullName { get; }
    public string ConverterName { get; }
    public string[] PropertyNames { get; }
    public ToonPrimitiveKind[] PropertyKinds { get; }

    public TabularArrayInfo(ITypeSymbol symbol)
    {
        ElementFullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        ConverterName = $"{ElementFullName.Replace("global::", "").Replace(".", "_")}TabularArrayConverter";

        var nameAndKinds = symbol.GetMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .OfType<IPropertySymbol>()
            .Select(p =>
            {
                return (p.Name, Kind: GetKind(p.Type));

                static ToonPrimitiveKind GetKind(ITypeSymbol t)
                {
                    if (t.NullableAnnotation == NullableAnnotation.Annotated && t is INamedTypeSymbol nts && nts.IsGenericType && nts.Name == "Nullable" && nts.TypeArguments.Length == 1)
                    {
                        var underlyingType = nts.TypeArguments[0];

                        if (underlyingType.TypeKind == TypeKind.Enum)
                        {
                            return ToonPrimitiveKind.NullableEnum;
                        }

                        if (IsGuid(underlyingType))
                        {
                            return ToonPrimitiveKind.NullableGuid;
                        }

                        return underlyingType.SpecialType switch
                        {
                            SpecialType.System_Boolean => ToonPrimitiveKind.NullableBoolean,
                            SpecialType.System_DateTime => ToonPrimitiveKind.NullableDateTime,
                            SpecialType.System_Byte or
                            SpecialType.System_SByte or
                            SpecialType.System_Int16 or
                            SpecialType.System_UInt16 or
                            SpecialType.System_Int32 or
                            SpecialType.System_UInt32 or
                            SpecialType.System_Int64 or
                            SpecialType.System_UInt64 or
                            SpecialType.System_Single or
                            SpecialType.System_Double or
                            SpecialType.System_Decimal => ToonPrimitiveKind.NullableNumber,
                            _ => ToonPrimitiveKind.Unsupported,
                        };
                    }
                    else
                    {
                        if (t.TypeKind == TypeKind.Enum)
                        {
                            return ToonPrimitiveKind.Enum;
                        }

                        if (IsGuid(t))
                        {
                            return ToonPrimitiveKind.Guid;
                        }

                        return t.SpecialType switch
                        {
                            SpecialType.System_Boolean => ToonPrimitiveKind.Boolean,
                            SpecialType.System_DateTime => ToonPrimitiveKind.DateTime,
                            SpecialType.System_String => ToonPrimitiveKind.String,
                            SpecialType.System_Byte or
                            SpecialType.System_SByte or
                            SpecialType.System_Int16 or
                            SpecialType.System_UInt16 or
                            SpecialType.System_Int32 or
                            SpecialType.System_UInt32 or
                            SpecialType.System_Int64 or
                            SpecialType.System_UInt64 or
                            SpecialType.System_Single or
                            SpecialType.System_Double or
                            SpecialType.System_Decimal => ToonPrimitiveKind.Number,
                            _ => ToonPrimitiveKind.Unsupported,
                        };
                    }
                }

                static bool IsGuid(ITypeSymbol type)
                {
                    return type is INamedTypeSymbol { Name: "Guid", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
                }
            })
            .ToArray();

        PropertyNames = nameAndKinds.Select(x => x.Name).ToArray();
        PropertyKinds = nameAndKinds.Select(x => x.Kind).ToArray();
    }
}

public enum ToonPrimitiveKind
{
    Boolean,
    String,
    Number,
    DateTime, // write as string
    Guid,     // write as string
    Enum,     // write as string

    NullableNumber,
    NullableBoolean,
    NullableDateTime,
    NullableGuid,
    NullableEnum,

    Unsupported
}