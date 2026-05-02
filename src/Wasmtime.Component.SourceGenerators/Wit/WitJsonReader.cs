using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Wasmtime.Component.SourceGenerators.Wit;

/// <summary>
/// Parses the JSON IR produced by <c>wasm-tools component wit X.wit --json</c> into <see cref="WitModel"/>.
/// </summary>
internal static class WitJsonReader
{
    public static WitModel Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new WitModel(
            Worlds: ReadWorlds(root),
            Types: ReadTypes(root),
            Packages: ReadPackages(root));
    }

    private static IReadOnlyList<WitWorldDef> ReadWorlds(JsonElement root)
    {
        if (!root.TryGetProperty("worlds", out var worlds) || worlds.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<WitWorldDef>();
        }

        var list = new List<WitWorldDef>();
        foreach (var w in worlds.EnumerateArray())
        {
            list.Add(new WitWorldDef(
                Name: w.GetProperty("name").GetString() ?? string.Empty,
                Imports: ReadWorldItemMap(w, "imports"),
                Exports: ReadWorldItemMap(w, "exports")));
        }
        return list;
    }

    private static IReadOnlyList<WitWorldItem> ReadWorldItemMap(JsonElement world, string property)
    {
        if (!world.TryGetProperty(property, out var map) || map.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<WitWorldItem>();
        }

        var items = new List<WitWorldItem>();
        foreach (var entry in map.EnumerateObject())
        {
            items.Add(new WitWorldItem(entry.Name, ReadWorldItemKind(entry.Value)));
        }
        return items;
    }

    private static WitWorldItemKind ReadWorldItemKind(JsonElement element)
    {
        if (element.TryGetProperty("function", out var fn))
        {
            return new WitWorldItemFunction(ReadFunction(fn));
        }

        if (element.TryGetProperty("type", out var typeRef))
        {
            return new WitWorldItemTypeRef(ReadTypeRef(typeRef));
        }

        if (element.TryGetProperty("interface", out _))
        {
            return new WitWorldItemTypeRef(new WitTypeRefPrimitive("interface"));
        }

        return new WitWorldItemTypeRef(new WitTypeRefPrimitive("unknown"));
    }

    private static WitFunction ReadFunction(JsonElement element)
    {
        var name = element.GetProperty("name").GetString() ?? string.Empty;
        var kind = element.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String
            ? k.GetString() ?? "freestanding"
            : "freestanding";

        var paramList = new List<WitParam>();
        if (element.TryGetProperty("params", out var pars) && pars.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in pars.EnumerateArray())
            {
                paramList.Add(new WitParam(
                    Name: p.GetProperty("name").GetString() ?? string.Empty,
                    Type: ReadTypeRef(p.GetProperty("type"))));
            }
        }

        WitTypeRef? result = null;
        if (element.TryGetProperty("result", out var res) && res.ValueKind != JsonValueKind.Null)
        {
            result = ReadTypeRef(res);
        }

        return new WitFunction(name, kind, paramList, result);
    }

    private static IReadOnlyList<WitTypeDef> ReadTypes(JsonElement root)
    {
        if (!root.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<WitTypeDef>();
        }

        var list = new List<WitTypeDef>();
        var index = 0;
        foreach (var t in types.EnumerateArray())
        {
            list.Add(new WitTypeDef(
                Index: index++,
                Name: t.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null,
                Kind: ReadKind(t.GetProperty("kind"))));
        }
        return list;
    }

    private static WitKind ReadKind(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return new WitTypeKindAlias(new WitTypeRefPrimitive(element.GetString()!));
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return new WitUnknownKind(element.ValueKind.ToString());
        }

        foreach (var prop in element.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "record":
                    return new WitRecordKind(ReadRecordFields(prop.Value));
                case "enum":
                    return new WitEnumKind(ReadCaseNames(prop.Value, "cases"));
                case "flags":
                    return new WitFlagsKind(ReadCaseNames(prop.Value, "flags"));
                case "variant":
                    return new WitVariantKind(ReadVariantCases(prop.Value));
                case "list":
                    return new WitListKind(ReadTypeRef(prop.Value));
                case "option":
                    return new WitOptionKind(ReadTypeRef(prop.Value));
                case "result":
                    return ReadResult(prop.Value);
                case "tuple":
                    return new WitTupleKind(ReadTupleTypes(prop.Value));
                case "type":
                    return new WitTypeKindAlias(ReadTypeRef(prop.Value));
                default:
                    return new WitUnknownKind(prop.Name);
            }
        }

        return new WitUnknownKind("(empty)");
    }

    private static IReadOnlyList<WitRecordField> ReadRecordFields(JsonElement element)
    {
        var fields = new List<WitRecordField>();
        if (element.TryGetProperty("fields", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in arr.EnumerateArray())
            {
                fields.Add(new WitRecordField(
                    Name: f.GetProperty("name").GetString() ?? string.Empty,
                    Type: ReadTypeRef(f.GetProperty("type"))));
            }
        }
        return fields;
    }

    private static IReadOnlyList<string> ReadCaseNames(JsonElement element, string property)
    {
        var names = new List<string>();
        if (element.TryGetProperty(property, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in arr.EnumerateArray())
            {
                names.Add(c.GetProperty("name").GetString() ?? string.Empty);
            }
        }
        return names;
    }

    private static IReadOnlyList<WitVariantCase> ReadVariantCases(JsonElement element)
    {
        var cases = new List<WitVariantCase>();
        if (element.TryGetProperty("cases", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in arr.EnumerateArray())
            {
                WitTypeRef? payload = null;
                if (c.TryGetProperty("type", out var t) && t.ValueKind != JsonValueKind.Null)
                {
                    payload = ReadTypeRef(t);
                }
                cases.Add(new WitVariantCase(
                    Name: c.GetProperty("name").GetString() ?? string.Empty,
                    Payload: payload));
            }
        }
        return cases;
    }

    private static WitResultKind ReadResult(JsonElement element)
    {
        WitTypeRef? ok = null;
        WitTypeRef? err = null;
        if (element.TryGetProperty("ok", out var o) && o.ValueKind != JsonValueKind.Null)
        {
            ok = ReadTypeRef(o);
        }
        if (element.TryGetProperty("err", out var e) && e.ValueKind != JsonValueKind.Null)
        {
            err = ReadTypeRef(e);
        }
        return new WitResultKind(ok, err);
    }

    private static IReadOnlyList<WitTypeRef> ReadTupleTypes(JsonElement element)
    {
        var types = new List<WitTypeRef>();
        if (element.TryGetProperty("types", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in arr.EnumerateArray())
            {
                types.Add(ReadTypeRef(t));
            }
        }
        return types;
    }

    private static WitTypeRef ReadTypeRef(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => new WitTypeRefIndex(element.GetInt32()),
            JsonValueKind.String => new WitTypeRefPrimitive(element.GetString()!),
            _ => new WitTypeRefPrimitive("unknown"),
        };
    }

    private static IReadOnlyList<WitPackage> ReadPackages(JsonElement root)
    {
        if (!root.TryGetProperty("packages", out var packages) || packages.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<WitPackage>();
        }

        var list = new List<WitPackage>();
        foreach (var p in packages.EnumerateArray())
        {
            list.Add(new WitPackage(p.GetProperty("name").GetString() ?? string.Empty));
        }
        return list;
    }
}
