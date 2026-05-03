using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wasmtime.Component.SourceGenerators.Wit;

namespace Wasmtime.Component.SourceGenerators.Emit;

/// <summary>
/// Emits C# methods that invoke component exports plus the per-named-type lift/lower helpers
/// they delegate to.
/// </summary>
internal static class FunctionEmitter
{
    private const string Cv = "Wasmtime.Components.ComponentValue";
    private const string Rf = "Wasmtime.Components.RecordField";
    private const string Result = "Wasmtime.Components.Result";

    public static void EmitMethods(
        StringBuilder sb,
        string className,
        WitWorldDef world,
        WitModel model,
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

        EmitNamedTypeHelpers(sb, model, ctx, indent);

        var importFns = world.Imports
            .Where(i => i.Kind is WitWorldItemFunction)
            .Select(i => (Name: i.Name, Function: ((WitWorldItemFunction)i.Kind).Function))
            .ToList();
        if (importFns.Count > 0)
        {
            EmitImportsInterface(sb, importFns, ctx, indent);
            EmitRegisterImports(sb, importFns, ctx, indent);
        }

        foreach (var item in world.Exports)
        {
            if (item.Kind is not WitWorldItemFunction fn)
            {
                continue;
            }

            EmitMethod(sb, item.Name, fn.Function, ctx, indent);
        }
    }

    /// <summary>
    /// Emits the user-implementable <c>IImports</c> interface for the world's imported functions.
    /// </summary>
    private static void EmitImportsInterface(
        StringBuilder sb,
        IReadOnlyList<(string Name, WitFunction Function)> imports,
        EmitContext ctx,
        string indent)
    {
        sb.Append(indent).AppendLine("public interface IImports");
        sb.Append(indent).AppendLine("{");
        foreach (var (name, fn) in imports)
        {
            var methodName = EmitContext.ToPascalCase(fn.Name);
            var resultType = fn.Result is null ? "void" : ctx.ResolveTypeRef(fn.Result);
            sb.Append(indent).Append("    ").Append(resultType).Append(' ').Append(methodName).Append('(');
            for (var i = 0; i < fn.Params.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(ctx.ResolveTypeRef(fn.Params[i].Type)).Append(' ').Append(EmitContext.ToCamelCase(fn.Params[i].Name));
            }
            sb.AppendLine(");");
        }
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// Emits a static <c>RegisterImports</c> method that wires every <c>IImports</c> member to a
    /// <c>ComponentLinker.Root().DefineFunc(...)</c> callback so the host implementation runs when
    /// the component invokes the matching import.
    /// </summary>
    private static void EmitRegisterImports(
        StringBuilder sb,
        IReadOnlyList<(string Name, WitFunction Function)> imports,
        EmitContext ctx,
        string indent)
    {
        sb.Append(indent).AppendLine("public static void RegisterImports(Wasmtime.Components.ComponentLinker linker, IImports impl)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    if (linker is null) throw new System.ArgumentNullException(nameof(linker));");
        sb.Append(indent).AppendLine("    if (impl is null) throw new System.ArgumentNullException(nameof(impl));");
        sb.Append(indent).AppendLine("    var root = linker.Root();");
        sb.Append(indent).AppendLine("    try");
        sb.Append(indent).AppendLine("    {");
        foreach (var (name, fn) in imports)
        {
            var methodName = EmitContext.ToPascalCase(fn.Name);
            sb.Append(indent).Append("        root.DefineFunc(\"").Append(EscapeString(name)).AppendLine("\", (args, results) =>");
            sb.Append(indent).AppendLine("        {");

            for (var i = 0; i < fn.Params.Count; i++)
            {
                var paramType = ctx.ResolveTypeRef(fn.Params[i].Type);
                sb.Append(indent).Append("            ").Append(paramType).Append(" arg").Append(i).Append(" = ")
                    .Append(LiftExpr(fn.Params[i].Type, $"args[{i}]", ctx)).AppendLine(";");
            }

            if (fn.Result is null)
            {
                sb.Append(indent).Append("            impl.").Append(methodName).Append('(');
                for (var i = 0; i < fn.Params.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append("arg").Append(i);
                }
                sb.AppendLine(");");
            }
            else
            {
                var resultType = ctx.ResolveTypeRef(fn.Result);
                sb.Append(indent).Append("            ").Append(resultType).Append(" hostResult = impl.").Append(methodName).Append('(');
                for (var i = 0; i < fn.Params.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append("arg").Append(i);
                }
                sb.AppendLine(");");
                sb.Append(indent).Append("            results[0] = ").Append(LowerExpr(fn.Result, "hostResult", ctx)).AppendLine(";");
            }

            sb.Append(indent).AppendLine("        });");
        }
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("    finally");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        root.Dispose();");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static void EmitMethod(
        StringBuilder sb,
        string exportName,
        WitFunction function,
        EmitContext ctx,
        string indent)
    {
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

        sb.Append(indent).Append("    var args = new ").Append(Cv).Append('[').Append(function.Params.Count).AppendLine("];");
        for (var i = 0; i < function.Params.Count; i++)
        {
            var paramName = EmitContext.ToCamelCase(function.Params[i].Name);
            sb.Append(indent).Append("    args[").Append(i).Append("] = ").Append(LowerExpr(function.Params[i].Type, paramName, ctx)).AppendLine(";");
        }

        var hasResult = function.Result is not null;
        sb.Append(indent).Append("    var rets = new ").Append(Cv).Append('[').Append(hasResult ? 1 : 0).AppendLine("];");
        sb.Append(indent).AppendLine("    try");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        func.Call(args, rets);");

        if (hasResult)
        {
            sb.Append(indent).Append("        return ").Append(LiftExpr(function.Result!, "rets[0]", ctx)).AppendLine(";");
        }

        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("    finally");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        for (var i = 0; i < args.Length; i++) args[i].Free();");
        sb.Append(indent).AppendLine("        for (var i = 0; i < rets.Length; i++) rets[i].Free();");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// Emits static helpers <c>LowerXxx(Xxx)</c> / <c>LiftXxx(ComponentValue)</c> for every
    /// named WIT type so per-function emission can delegate to them and avoid inlining.
    /// </summary>
    private static void EmitNamedTypeHelpers(StringBuilder sb, WitModel model, EmitContext ctx, string indent)
    {
        foreach (var type in model.Types)
        {
            if (type.Name is null)
            {
                continue;
            }

            switch (type.Kind)
            {
                case WitRecordKind record:
                    EmitRecordHelpers(sb, type.Name, record, ctx, indent);
                    break;
                case WitEnumKind @enum:
                    EmitEnumHelpers(sb, type.Name, @enum, indent);
                    break;
                case WitFlagsKind flags:
                    EmitFlagsHelpers(sb, type.Name, flags, indent);
                    break;
                case WitVariantKind variant:
                    EmitVariantHelpers(sb, type.Name, variant, ctx, indent);
                    break;
            }
        }
    }

    private static void EmitRecordHelpers(StringBuilder sb, string name, WitRecordKind record, EmitContext ctx, string indent)
    {
        var pascal = EmitContext.ToPascalCase(name);

        sb.Append(indent).Append("private static ").Append(Cv).Append(" Lower").Append(pascal).Append('(').Append(pascal).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    return ").Append(Cv).AppendLine(".FromRecord(new[]");
        sb.Append(indent).AppendLine("    {");
        foreach (var field in record.Fields)
        {
            sb.Append(indent).Append("        new ").Append(Rf).Append("(\"").Append(EscapeString(field.Name)).Append("\", ")
                .Append(LowerExpr(field.Type, "value." + EmitContext.ToPascalCase(field.Name), ctx))
                .AppendLine("),");
        }
        sb.Append(indent).AppendLine("    });");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();

        sb.Append(indent).Append("private static ").Append(pascal).Append(" Lift").Append(pascal).Append('(').Append(Cv).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    var fields = value.AsRecord();");
        foreach (var field in record.Fields)
        {
            var fieldType = ctx.ResolveTypeRef(field.Type);
            sb.Append(indent).Append("    ").Append(fieldType).Append(' ').Append(EmitContext.ToCamelCase(field.Name)).Append(" = default!;").AppendLine();
        }
        sb.Append(indent).AppendLine("    foreach (var f in fields)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        switch (f.Name)");
        sb.Append(indent).AppendLine("        {");
        foreach (var field in record.Fields)
        {
            sb.Append(indent).Append("            case \"").Append(EscapeString(field.Name)).Append("\": ")
                .Append(EmitContext.ToCamelCase(field.Name)).Append(" = ")
                .Append(LiftExpr(field.Type, "f.Value", ctx)).AppendLine("; break;");
        }
        sb.Append(indent).AppendLine("        }");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).Append("    return new ").Append(pascal).Append('(');
        for (var i = 0; i < record.Fields.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(EmitContext.ToCamelCase(record.Fields[i].Name));
        }
        sb.AppendLine(");");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static void EmitEnumHelpers(StringBuilder sb, string name, WitEnumKind @enum, string indent)
    {
        var pascal = EmitContext.ToPascalCase(name);

        sb.Append(indent).Append("private static ").Append(Cv).Append(" Lower").Append(pascal).Append('(').Append(pascal).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    return value switch");
        sb.Append(indent).AppendLine("    {");
        foreach (var c in @enum.Cases)
        {
            sb.Append(indent).Append("        ").Append(pascal).Append('.').Append(EmitContext.ToPascalCase(c)).Append(" => ")
                .Append(Cv).Append(".FromEnum(\"").Append(EscapeString(c)).AppendLine("\"),");
        }
        sb.Append(indent).Append("        _ => throw new System.ArgumentOutOfRangeException(nameof(value)),").AppendLine();
        sb.Append(indent).AppendLine("    };");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();

        sb.Append(indent).Append("private static ").Append(pascal).Append(" Lift").Append(pascal).Append('(').Append(Cv).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    return value.AsEnum() switch");
        sb.Append(indent).AppendLine("    {");
        foreach (var c in @enum.Cases)
        {
            sb.Append(indent).Append("        \"").Append(EscapeString(c)).Append("\" => ").Append(pascal).Append('.').Append(EmitContext.ToPascalCase(c)).AppendLine(",");
        }
        sb.Append(indent).Append("        var other => throw new System.InvalidOperationException($\"Unknown enum case: {other}\"),").AppendLine();
        sb.Append(indent).AppendLine("    };");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static void EmitFlagsHelpers(StringBuilder sb, string name, WitFlagsKind flags, string indent)
    {
        var pascal = EmitContext.ToPascalCase(name);

        sb.Append(indent).Append("private static ").Append(Cv).Append(" Lower").Append(pascal).Append('(').Append(pascal).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    var names = new System.Collections.Generic.List<string>();");
        foreach (var f in flags.Flags)
        {
            sb.Append(indent).Append("    if ((value & ").Append(pascal).Append('.').Append(EmitContext.ToPascalCase(f)).Append(") != 0) names.Add(\"")
                .Append(EscapeString(f)).AppendLine("\");");
        }
        sb.Append(indent).Append("    return ").Append(Cv).AppendLine(".FromFlags(names);");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();

        sb.Append(indent).Append("private static ").Append(pascal).Append(" Lift").Append(pascal).Append('(').Append(Cv).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    var result = ").Append(pascal).AppendLine(".None;");
        sb.Append(indent).AppendLine("    foreach (var name in value.AsFlags())");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        switch (name)");
        sb.Append(indent).AppendLine("        {");
        foreach (var f in flags.Flags)
        {
            sb.Append(indent).Append("            case \"").Append(EscapeString(f)).Append("\": result |= ")
                .Append(pascal).Append('.').Append(EmitContext.ToPascalCase(f)).AppendLine("; break;");
        }
        sb.Append(indent).AppendLine("        }");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("    return result;");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static void EmitVariantHelpers(StringBuilder sb, string name, WitVariantKind variant, EmitContext ctx, string indent)
    {
        var pascal = EmitContext.ToPascalCase(name);

        sb.Append(indent).Append("private static ").Append(Cv).Append(" Lower").Append(pascal).Append('(').Append(pascal).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    return value switch");
        sb.Append(indent).AppendLine("    {");
        foreach (var c in variant.Cases)
        {
            var caseName = EmitContext.ToPascalCase(c.Name);
            sb.Append(indent).Append("        ").Append(pascal).Append('.').Append(caseName);
            if (c.Payload is not null)
            {
                sb.Append(" v => ").Append(Cv).Append(".FromVariant(\"").Append(EscapeString(c.Name)).Append("\", ")
                    .Append(LowerExpr(c.Payload, "v.Value", ctx)).AppendLine("),");
            }
            else
            {
                sb.Append(" => ").Append(Cv).Append(".FromVariant(\"").Append(EscapeString(c.Name)).AppendLine("\"),");
            }
        }
        sb.Append(indent).Append("        _ => throw new System.ArgumentOutOfRangeException(nameof(value)),").AppendLine();
        sb.Append(indent).AppendLine("    };");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();

        sb.Append(indent).Append("private static ").Append(pascal).Append(" Lift").Append(pascal).Append('(').Append(Cv).Append(" value)").AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    var disc = value.AsVariantDiscriminant();");
        sb.Append(indent).AppendLine("    var payload = value.AsVariantPayload();");
        sb.Append(indent).AppendLine("    return disc switch");
        sb.Append(indent).AppendLine("    {");
        foreach (var c in variant.Cases)
        {
            var caseName = EmitContext.ToPascalCase(c.Name);
            sb.Append(indent).Append("        \"").Append(EscapeString(c.Name)).Append("\" => ");
            if (c.Payload is not null)
            {
                sb.Append("new ").Append(pascal).Append('.').Append(caseName).Append('(')
                    .Append(LiftExpr(c.Payload, "payload!.Value", ctx)).AppendLine("),");
            }
            else
            {
                sb.Append("new ").Append(pascal).Append('.').Append(caseName).AppendLine("(),");
            }
        }
        sb.Append(indent).Append("        var other => throw new System.InvalidOperationException($\"Unknown variant case: {other}\"),").AppendLine();
        sb.Append(indent).AppendLine("    };");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static string LowerExpr(WitTypeRef typeRef, string variable, EmitContext ctx)
    {
        if (typeRef is WitTypeRefPrimitive p)
        {
            return LowerPrimitive(p.Name, variable);
        }

        if (typeRef is WitTypeRefIndex idx)
        {
            var def = ctx.GetTypeDef(idx.Index);
            if (def is null)
            {
                return $"throw new System.NotSupportedException()";
            }

            if (def.Name is not null)
            {
                return $"Lower{EmitContext.ToPascalCase(def.Name)}({variable})";
            }

            return def.Kind switch
            {
                WitListKind list => LowerList(list, variable, ctx),
                WitOptionKind option => LowerOption(option, variable, ctx),
                WitResultKind result => LowerResult(result, variable, ctx),
                WitTupleKind tuple => LowerTuple(tuple, variable, ctx),
                _ => $"throw new System.NotSupportedException()",
            };
        }

        return $"throw new System.NotSupportedException()";
    }

    private static string LiftExpr(WitTypeRef typeRef, string source, EmitContext ctx)
    {
        if (typeRef is WitTypeRefPrimitive p)
        {
            return LiftPrimitive(p.Name, source);
        }

        if (typeRef is WitTypeRefIndex idx)
        {
            var def = ctx.GetTypeDef(idx.Index);
            if (def is null)
            {
                return $"throw new System.NotSupportedException()";
            }

            if (def.Name is not null)
            {
                return $"Lift{EmitContext.ToPascalCase(def.Name)}({source})";
            }

            return def.Kind switch
            {
                WitListKind list => LiftList(list, source, ctx),
                WitOptionKind option => LiftOption(option, source, ctx),
                WitResultKind result => LiftResult(result, source, ctx),
                WitTupleKind tuple => LiftTuple(tuple, source, ctx),
                _ => $"throw new System.NotSupportedException()",
            };
        }

        return $"throw new System.NotSupportedException()";
    }

    private static string LowerList(WitListKind list, string variable, EmitContext ctx)
    {
        var elemType = ctx.ResolveTypeRef(list.Element);
        return $"{Cv}.FromList(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select<{elemType}, {Cv}>({variable}, e => {LowerExpr(list.Element, "e", ctx)})))";
    }

    private static string LiftList(WitListKind list, string source, EmitContext ctx)
    {
        var elemType = ctx.ResolveTypeRef(list.Element);
        return $"System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select<{Cv}, {elemType}>({source}.AsList(), e => {LiftExpr(list.Element, "e", ctx)}))";
    }

    private static string LowerOption(WitOptionKind option, string variable, EmitContext ctx)
    {
        // For option<option<T>> the C# variable is `Option<T?>`, accessed via .HasValue / .Value.
        if (ctx.IsOptionType(option.Element))
        {
            var inner = LowerExpr(option.Element, variable + ".Value", ctx);
            return $"({variable}.HasValue ? {Cv}.FromOption({inner}) : {Cv}.FromOption(null))";
        }

        // Single-level option: variable is `T?` (Nullable<T> for value types, nullable annotation otherwise).
        if (IsValueType(option.Element, ctx))
        {
            var inner = LowerExpr(option.Element, variable + ".Value", ctx);
            return $"({variable}.HasValue ? {Cv}.FromOption({inner}) : {Cv}.FromOption(null))";
        }

        var refInner = LowerExpr(option.Element, variable + "!", ctx);
        return $"({variable} is null ? {Cv}.FromOption(null) : {Cv}.FromOption({refInner}))";
    }

    private static string LiftOption(WitOptionKind option, string source, EmitContext ctx)
    {
        var inner = LiftExpr(option.Element, source + ".AsOption()!.Value", ctx);
        var elemType = ctx.ResolveTypeRef(option.Element);

        if (ctx.IsOptionType(option.Element))
        {
            // elemType is already `Wasmtime.Components.Option<...>`; wrap that in another Option<T>.
            var fullType = $"Wasmtime.Components.Option<{elemType}>";
            return $"({source}.HasOption() ? {fullType}.Some({inner}) : {fullType}.None)";
        }

        if (IsValueType(option.Element, ctx))
        {
            return $"({source}.HasOption() ? ({elemType}?){inner} : null)";
        }

        return $"({source}.HasOption() ? {inner} : null)";
    }

    private static string LowerResult(WitResultKind result, string variable, EmitContext ctx)
    {
        var okExpr = result.Ok is null
            ? $"{Cv}.FromOk()"
            : $"{Cv}.FromOk({LowerExpr(result.Ok, variable + ".Value", ctx)})";
        var errExpr = result.Err is null
            ? $"{Cv}.FromErr()"
            : $"{Cv}.FromErr({LowerExpr(result.Err, variable + ".Error", ctx)})";
        return $"({variable}.IsOk ? {okExpr} : {errExpr})";
    }

    private static string LiftResult(WitResultKind result, string source, EmitContext ctx)
    {
        var okType = result.Ok is null ? "Wasmtime.Components.Unit" : ctx.ResolveTypeRef(result.Ok);
        var errType = result.Err is null ? "Wasmtime.Components.Unit" : ctx.ResolveTypeRef(result.Err);
        var okValue = result.Ok is null
            ? "default(Wasmtime.Components.Unit)"
            : LiftExpr(result.Ok, source + ".AsResultValue()!.Value", ctx);
        var errValue = result.Err is null
            ? "default(Wasmtime.Components.Unit)"
            : LiftExpr(result.Err, source + ".AsResultValue()!.Value", ctx);
        return $"({source}.IsOk() ? {Result}<{okType}, {errType}>.Ok({okValue}) : {Result}<{okType}, {errType}>.Err({errValue}))";
    }

    private static string LowerTuple(WitTupleKind tuple, string variable, EmitContext ctx)
    {
        var sb = new StringBuilder();
        sb.Append(Cv).Append(".FromTuple(new[] { ");
        for (var i = 0; i < tuple.Elements.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(LowerExpr(tuple.Elements[i], $"{variable}.Item{i + 1}", ctx));
        }
        sb.Append(" })");
        return sb.ToString();
    }

    private static string LiftTuple(WitTupleKind tuple, string source, EmitContext ctx)
    {
        var sb = new StringBuilder();
        sb.Append('(');
        for (var i = 0; i < tuple.Elements.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(LiftExpr(tuple.Elements[i], $"{source}.AsTuple()[{i}]", ctx));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static bool IsValueType(WitTypeRef typeRef, EmitContext ctx)
    {
        if (typeRef is WitTypeRefPrimitive p)
        {
            return p.Name != "string";
        }

        if (typeRef is WitTypeRefIndex idx)
        {
            var def = ctx.GetTypeDef(idx.Index);
            return def?.Kind is WitEnumKind or WitFlagsKind;
        }

        return false;
    }

    private static string LowerPrimitive(string name, string variable) => name switch
    {
        "bool" => $"{Cv}.FromBool({variable})",
        "s8" => $"{Cv}.FromS8({variable})",
        "u8" => $"{Cv}.FromU8({variable})",
        "s16" => $"{Cv}.FromS16({variable})",
        "u16" => $"{Cv}.FromU16({variable})",
        "s32" => $"{Cv}.FromS32({variable})",
        "u32" => $"{Cv}.FromU32({variable})",
        "s64" => $"{Cv}.FromS64({variable})",
        "u64" => $"{Cv}.FromU64({variable})",
        "f32" => $"{Cv}.FromF32({variable})",
        "f64" => $"{Cv}.FromF64({variable})",
        "char" => $"{Cv}.FromChar({variable})",
        "string" => $"{Cv}.FromString({variable})",
        _ => $"throw new System.NotSupportedException(\"primitive {name}\")",
    };

    private static string LiftPrimitive(string name, string source) => name switch
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
        _ => $"throw new System.NotSupportedException(\"primitive {name}\")",
    };

    private static string EscapeString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    public static void EmitInfrastructure(StringBuilder _, string __)
    {
        // kept for backwards compatibility with the previous wiring; nothing to emit here now.
    }
}
