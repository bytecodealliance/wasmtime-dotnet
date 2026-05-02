using System.Linq;
using System.Text;
using Wasmtime.Component.SourceGenerators.Wit;

namespace Wasmtime.Component.SourceGenerators.Emit;

/// <summary>
/// Emits C# declarations for WIT type definitions (record / enum / flags / variant).
/// </summary>
/// <remarks>
/// All generated types live as nested members of the <c>partial class</c> annotated with
/// <c>[ComponentBindings]</c>. This keeps the surface tied to the bindings entry point
/// (<c>FixtureBindings.Point</c>, <c>FixtureBindings.Greeting.Formal</c>) rather than
/// polluting the user's namespace.
/// </remarks>
internal static class TypeEmitter
{
    public static void EmitNamedTypes(StringBuilder sb, WitModel model, EmitContext ctx, int indent)
    {
        var pad = new string(' ', indent);
        foreach (var type in model.Types)
        {
            if (type.Name is null)
            {
                continue;
            }

            switch (type.Kind)
            {
                case WitRecordKind record:
                    EmitRecord(sb, type.Name, record, ctx, pad);
                    break;
                case WitEnumKind @enum:
                    EmitEnum(sb, type.Name, @enum, pad);
                    break;
                case WitFlagsKind flags:
                    EmitFlags(sb, type.Name, flags, pad);
                    break;
                case WitVariantKind variant:
                    EmitVariant(sb, type.Name, variant, ctx, pad);
                    break;
            }
        }
    }

    private static void EmitRecord(StringBuilder sb, string name, WitRecordKind record, EmitContext ctx, string pad)
    {
        var typeName = EmitContext.ToPascalCase(name);
        sb.Append(pad).Append("public sealed record class ").Append(typeName).Append('(');

        for (var i = 0; i < record.Fields.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            var field = record.Fields[i];
            sb.Append(ctx.ResolveTypeRef(field.Type)).Append(' ').Append(EmitContext.ToPascalCase(field.Name));
        }

        sb.AppendLine(");");
    }

    private static void EmitEnum(StringBuilder sb, string name, WitEnumKind @enum, string pad)
    {
        var typeName = EmitContext.ToPascalCase(name);
        var backing = @enum.Cases.Count switch
        {
            <= 256 => "byte",
            <= 65536 => "ushort",
            _ => "uint",
        };

        sb.Append(pad).Append("public enum ").Append(typeName).Append(" : ").Append(backing).AppendLine();
        sb.Append(pad).AppendLine("{");
        for (var i = 0; i < @enum.Cases.Count; i++)
        {
            sb.Append(pad).Append("    ").Append(EmitContext.ToPascalCase(@enum.Cases[i])).Append(" = ").Append(i).AppendLine(",");
        }
        sb.Append(pad).AppendLine("}");
    }

    private static void EmitFlags(StringBuilder sb, string name, WitFlagsKind flags, string pad)
    {
        var typeName = EmitContext.ToPascalCase(name);
        var backing = flags.Flags.Count switch
        {
            <= 8 => "byte",
            <= 16 => "ushort",
            <= 32 => "uint",
            _ => "ulong",
        };

        sb.Append(pad).AppendLine("[System.Flags]");
        sb.Append(pad).Append("public enum ").Append(typeName).Append(" : ").Append(backing).AppendLine();
        sb.Append(pad).AppendLine("{");
        sb.Append(pad).AppendLine("    None = 0,");
        for (var i = 0; i < flags.Flags.Count; i++)
        {
            var bit = 1UL << i;
            sb.Append(pad).Append("    ").Append(EmitContext.ToPascalCase(flags.Flags[i])).Append(" = ").Append(bit).AppendLine(",");
        }
        sb.Append(pad).AppendLine("}");
    }

    private static void EmitVariant(StringBuilder sb, string name, WitVariantKind variant, EmitContext ctx, string pad)
    {
        var typeName = EmitContext.ToPascalCase(name);
        sb.Append(pad).Append("public abstract record ").Append(typeName).AppendLine();
        sb.Append(pad).AppendLine("{");
        sb.Append(pad).Append("    private ").Append(typeName).AppendLine("() { }");
        sb.AppendLine();

        foreach (var c in variant.Cases)
        {
            var caseName = EmitContext.ToPascalCase(c.Name);
            sb.Append(pad).Append("    public sealed record ").Append(caseName);
            if (c.Payload is not null)
            {
                sb.Append('(').Append(ctx.ResolveTypeRef(c.Payload)).Append(" Value)");
            }
            else
            {
                sb.Append("()");
            }
            sb.Append(" : ").Append(typeName).AppendLine(";");
        }

        sb.Append(pad).AppendLine("}");
    }
}
