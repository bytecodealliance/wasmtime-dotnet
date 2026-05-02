using System.Collections.Generic;

namespace Wasmtime.Component.SourceGenerators.Wit;

/// <summary>
/// Top-level model parsed from <c>wasm-tools component wit X.wit --json</c>.
/// </summary>
internal sealed record WitModel(
    IReadOnlyList<WitWorldDef> Worlds,
    IReadOnlyList<WitTypeDef> Types,
    IReadOnlyList<WitPackage> Packages);

internal sealed record WitWorldDef(
    string Name,
    IReadOnlyList<WitWorldItem> Imports,
    IReadOnlyList<WitWorldItem> Exports);

/// <summary>An entry in a world's <c>imports</c> or <c>exports</c> map.</summary>
internal sealed record WitWorldItem(
    string Name,
    WitWorldItemKind Kind);

internal abstract record WitWorldItemKind;

/// <summary>The world item references a type (e.g. an exported record/enum/variant alias).</summary>
internal sealed record WitWorldItemTypeRef(WitTypeRef Type) : WitWorldItemKind;

/// <summary>The world item is a freestanding function.</summary>
internal sealed record WitWorldItemFunction(WitFunction Function) : WitWorldItemKind;

internal sealed record WitFunction(
    string Name,
    string Kind,
    IReadOnlyList<WitParam> Params,
    WitTypeRef? Result);

internal sealed record WitParam(string Name, WitTypeRef Type);

/// <summary>
/// A type definition in <c>types[]</c>. Anonymous types (list/option/result/tuple) have a null name.
/// </summary>
internal sealed record WitTypeDef(
    int Index,
    string? Name,
    WitKind Kind);

internal abstract record WitKind;
internal sealed record WitRecordKind(IReadOnlyList<WitRecordField> Fields) : WitKind;
internal sealed record WitRecordField(string Name, WitTypeRef Type);
internal sealed record WitEnumKind(IReadOnlyList<string> Cases) : WitKind;
internal sealed record WitFlagsKind(IReadOnlyList<string> Flags) : WitKind;
internal sealed record WitVariantKind(IReadOnlyList<WitVariantCase> Cases) : WitKind;
internal sealed record WitVariantCase(string Name, WitTypeRef? Payload);
internal sealed record WitListKind(WitTypeRef Element) : WitKind;
internal sealed record WitOptionKind(WitTypeRef Element) : WitKind;
internal sealed record WitResultKind(WitTypeRef? Ok, WitTypeRef? Err) : WitKind;
internal sealed record WitTupleKind(IReadOnlyList<WitTypeRef> Elements) : WitKind;
internal sealed record WitTypeKindAlias(WitTypeRef Target) : WitKind;
internal sealed record WitUnknownKind(string KindName) : WitKind;

/// <summary>
/// A reference to a type — either an index into <see cref="WitModel.Types"/> or a primitive name.
/// </summary>
internal abstract record WitTypeRef;
internal sealed record WitTypeRefIndex(int Index) : WitTypeRef;
internal sealed record WitTypeRefPrimitive(string Name) : WitTypeRef;

internal sealed record WitPackage(string Name);
