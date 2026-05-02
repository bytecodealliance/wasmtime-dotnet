using Microsoft.CodeAnalysis;

namespace Wasmtime.Component.SourceGenerators.Diagnostics;

internal static class Descriptors
{
    private const string Category = "Wasmtime.Component";

    public static readonly DiagnosticDescriptor TargetMustBePartial = new(
        id: "WIT019",
        title: "[ComponentBindings] target class must be partial",
        messageFormat: "Class '{0}' has [ComponentBindings] but is not declared partial; the generator cannot extend it",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WitPathMissing = new(
        id: "WIT018",
        title: "[ComponentBindings] requires non-empty witPath",
        messageFormat: "[ComponentBindings] on '{0}' has no witPath argument",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WitFileNotRegistered = new(
        id: "WIT010",
        title: "WIT file not registered as <AdditionalFiles>",
        messageFormat: "WIT file '{0}' was not provided to the generator via <AdditionalFiles>; add it to the project",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GeneratedSummary = new(
        id: "WIT020",
        title: "Component bindings generated",
        messageFormat: "Generated bindings for '{0}' (world: {1})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
