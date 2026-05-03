using System;
using System.Collections.Generic;
using System.Text;
using Wasmtime.Component.SourceGenerators.Wit;

namespace Wasmtime.Component.SourceGenerators.Emit;

/// <summary>
/// Resolves type references during emission and converts WIT kebab-case identifiers to PascalCase
/// C# names.
/// </summary>
internal sealed class EmitContext
{
    private readonly IReadOnlyList<WitTypeDef> types;

    public EmitContext(IReadOnlyList<WitTypeDef> types)
    {
        this.types = types;
    }

    public WitTypeDef? GetTypeDef(int index)
    {
        if (index < 0 || index >= types.Count)
        {
            return null;
        }
        return types[index];
    }

    public string ResolveTypeRef(WitTypeRef typeRef)
    {
        return typeRef switch
        {
            WitTypeRefPrimitive p => MapPrimitive(p.Name),
            WitTypeRefIndex idx => ResolveIndex(idx.Index),
            _ => "object",
        };
    }

    private string ResolveIndex(int index)
    {
        if (index < 0 || index >= types.Count)
        {
            return "object";
        }

        var def = types[index];
        if (def.Name is not null)
        {
            return ToPascalCase(def.Name);
        }

        // Anonymous types — render their structural form.
        return def.Kind switch
        {
            WitListKind list => $"System.Collections.Generic.IReadOnlyList<{ResolveTypeRef(list.Element)}>",
            WitOptionKind option => MakeNullable(option.Element),
            WitResultKind result => RenderResult(result),
            WitTupleKind tuple => RenderTuple(tuple),
            _ => "object",
        };
    }

    private string MakeNullable(WitTypeRef element)
    {
        // option<option<T>> can't be `T??` — C# disallows double-nullable. Wrap with our own
        // Option<T> struct in those cases; single-level options stay as `T?` for ergonomics.
        if (IsOptionType(element))
        {
            var inner = ResolveTypeRef(element);
            return $"Wasmtime.Components.Option<{inner}>";
        }

        var nullable = ResolveTypeRef(element);
        return $"{nullable}?";
    }

    public bool IsOptionType(WitTypeRef typeRef)
    {
        if (typeRef is WitTypeRefIndex idx)
        {
            return GetTypeDef(idx.Index)?.Kind is WitOptionKind;
        }
        return false;
    }

    private string RenderResult(WitResultKind result)
    {
        var ok = result.Ok is null ? "Wasmtime.Components.Unit" : ResolveTypeRef(result.Ok);
        var err = result.Err is null ? "Wasmtime.Components.Unit" : ResolveTypeRef(result.Err);
        return $"Wasmtime.Components.Result<{ok}, {err}>";
    }

    private string RenderTuple(WitTupleKind tuple)
    {
        var sb = new StringBuilder("(");
        for (var i = 0; i < tuple.Elements.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(ResolveTypeRef(tuple.Elements[i]));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private bool IsValueType(WitTypeRef typeRef)
    {
        if (typeRef is WitTypeRefPrimitive p)
        {
            return p.Name switch
            {
                "string" => false,
                _ => true,
            };
        }

        if (typeRef is WitTypeRefIndex idx && idx.Index >= 0 && idx.Index < types.Count)
        {
            return types[idx.Index].Kind is WitEnumKind or WitFlagsKind;
        }

        return false;
    }

    public static string MapPrimitive(string name) => name switch
    {
        "bool" => "bool",
        "s8" => "sbyte",
        "u8" => "byte",
        "s16" => "short",
        "u16" => "ushort",
        "s32" => "int",
        "u32" => "uint",
        "s64" => "long",
        "u64" => "ulong",
        "f32" => "float",
        "f64" => "double",
        "char" => "uint",
        "string" => "string",
        _ => name,
    };

    public static string ToPascalCase(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        var sb = new StringBuilder(identifier.Length);
        var capitalizeNext = true;
        foreach (var ch in identifier)
        {
            if (ch is '-' or '_' or ' ')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(ch));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(ch);
            }
        }

        // Reserved keyword guard.
        var result = sb.ToString();
        return s_keywords.Contains(result) ? "@" + result : result;
    }

    public static string ToCamelCase(string identifier)
    {
        var pascal = ToPascalCase(identifier);
        if (pascal.Length == 0)
        {
            return pascal;
        }

        if (pascal[0] == '@')
        {
            return pascal;
        }

        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }

    private static readonly HashSet<string> s_keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    };
}
