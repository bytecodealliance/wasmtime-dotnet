using System.Text;
using Wasmtime.Component.SourceGenerators.Wit;

namespace Wasmtime.Component.SourceGenerators.Emit;

/// <summary>
/// Emits C# methods that invoke component exports. Currently restricted to functions whose
/// parameters and return types are all WIT primitives — composite arg/result lifting/lowering
/// will be wired up in a follow-up commit.
/// </summary>
internal static class FunctionEmitter
{
    public static void EmitInfrastructure(StringBuilder sb, string indent)
    {
        sb.Append(indent).AppendLine("private readonly Wasmtime.Components.ComponentInstance _instance;");
        sb.AppendLine();
        sb.Append(indent).AppendLine("public static T Bind<T>(Wasmtime.Components.ComponentInstance instance) where T : new() => throw new System.NotSupportedException();");
        sb.AppendLine();
    }

    /// <summary>
    /// Emits a constructor and per-export methods for the bindings class.
    /// </summary>
    public static void EmitMethods(
        StringBuilder sb,
        string className,
        WitWorldDef world,
        EmitContext ctx,
        string indent)
    {
        sb.Append(indent).Append("private readonly Wasmtime.Components.ComponentInstance _instance;").AppendLine();
        sb.AppendLine();
        sb.Append(indent).Append("public ").Append(className).AppendLine("(Wasmtime.Components.ComponentInstance instance)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    _instance = instance ?? throw new System.ArgumentNullException(nameof(instance));");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();

        foreach (var item in world.Exports)
        {
            if (item.Kind is not WitWorldItemFunction fn)
            {
                continue;
            }

            EmitMethod(sb, item.Name, fn.Function, ctx, indent);
        }
    }

    private static void EmitMethod(
        StringBuilder sb,
        string exportName,
        WitFunction function,
        EmitContext ctx,
        string indent)
    {
        if (!IsPrimitiveSignature(function))
        {
            sb.Append(indent).Append("// TODO: bindings for '").Append(exportName).AppendLine("' (composite signature) — pending FunctionEmitter step.");
            sb.AppendLine();
            return;
        }

        var methodName = EmitContext.ToPascalCase(function.Name);
        var resultType = function.Result is null ? "void" : ctx.ResolveTypeRef(function.Result);

        sb.Append(indent).Append("public ").Append(resultType).Append(' ').Append(methodName).Append('(');
        for (var i = 0; i < function.Params.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            var p = function.Params[i];
            sb.Append(ctx.ResolveTypeRef(p.Type)).Append(' ').Append(EmitContext.ToCamelCase(p.Name));
        }
        sb.Append(')').AppendLine();
        sb.Append(indent).AppendLine("{");

        sb.Append(indent).Append("    var func = _instance.GetFunction(\"").Append(EscapeString(exportName)).AppendLine("\")");
        sb.Append(indent).Append("        ?? throw new System.InvalidOperationException(\"Component does not export '").Append(EscapeString(exportName)).AppendLine("'.\");");

        sb.Append(indent).Append("    var args = new Wasmtime.Components.ComponentValue[").Append(function.Params.Count).AppendLine("];");
        for (var i = 0; i < function.Params.Count; i++)
        {
            sb.Append(indent).Append("    args[").Append(i).Append("] = ");
            sb.Append(LowerPrimitive(function.Params[i].Type, EmitContext.ToCamelCase(function.Params[i].Name))).AppendLine(";");
        }

        var hasResult = function.Result is not null;
        sb.Append(indent).Append("    var rets = new Wasmtime.Components.ComponentValue[").Append(hasResult ? 1 : 0).AppendLine("];");
        sb.Append(indent).AppendLine("    func.Call(args, rets);");

        if (hasResult)
        {
            sb.Append(indent).Append("    return ").Append(LiftPrimitive(function.Result!, "rets[0]")).AppendLine(";");
        }

        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static bool IsPrimitiveSignature(WitFunction function)
    {
        foreach (var p in function.Params)
        {
            if (!IsPrimitive(p.Type))
            {
                return false;
            }
        }

        if (function.Result is not null && !IsPrimitive(function.Result))
        {
            return false;
        }

        return true;
    }

    private static bool IsPrimitive(WitTypeRef typeRef)
    {
        return typeRef is WitTypeRefPrimitive p && p.Name switch
        {
            "bool" or "s8" or "u8" or "s16" or "u16"
                or "s32" or "u32" or "s64" or "u64"
                or "f32" or "f64" or "char" or "string" => true,
            _ => false,
        };
    }

    private static string LowerPrimitive(WitTypeRef typeRef, string variable)
    {
        var name = ((WitTypeRefPrimitive)typeRef).Name;
        return name switch
        {
            "bool" => $"Wasmtime.Components.ComponentValue.FromBool({variable})",
            "s8" => $"Wasmtime.Components.ComponentValue.FromS8({variable})",
            "u8" => $"Wasmtime.Components.ComponentValue.FromU8({variable})",
            "s16" => $"Wasmtime.Components.ComponentValue.FromS16({variable})",
            "u16" => $"Wasmtime.Components.ComponentValue.FromU16({variable})",
            "s32" => $"Wasmtime.Components.ComponentValue.FromS32({variable})",
            "u32" => $"Wasmtime.Components.ComponentValue.FromU32({variable})",
            "s64" => $"Wasmtime.Components.ComponentValue.FromS64({variable})",
            "u64" => $"Wasmtime.Components.ComponentValue.FromU64({variable})",
            "f32" => $"Wasmtime.Components.ComponentValue.FromF32({variable})",
            "f64" => $"Wasmtime.Components.ComponentValue.FromF64({variable})",
            "char" => $"Wasmtime.Components.ComponentValue.FromChar({variable})",
            "string" => $"Wasmtime.Components.ComponentValue.FromString({variable})",
            _ => $"throw new System.NotSupportedException(\"Cannot lower {name}\")",
        };
    }

    private static string LiftPrimitive(WitTypeRef typeRef, string source)
    {
        var name = ((WitTypeRefPrimitive)typeRef).Name;
        return name switch
        {
            "bool" => $"{source}.AsBool()",
            "s8" => $"{source}.AsS8()",
            "u8" => $"{source}.AsU8()",
            "s16" => $"{source}.AsS16()",
            "u16" => $"{source}.AsU16()",
            "s32" => $"{source}.AsS32()",
            "u32" => $"{source}.AsU32()",
            "s64" => $"{source}.AsS64()",
            "u64" => $"{source}.AsU64()",
            "f32" => $"{source}.AsF32()",
            "f64" => $"{source}.AsF64()",
            "char" => $"{source}.AsChar()",
            "string" => $"{source}.AsString()",
            _ => $"throw new System.NotSupportedException(\"Cannot lift {name}\")",
        };
    }

    private static string EscapeString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
