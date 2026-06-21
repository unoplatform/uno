---
description: Roslyn source-generator architecture, conventions, and performance constraints for Uno.UI.SourceGenerators. Auto-loaded when editing generators.
paths:
  - "src/SourceGenerators/**/*.cs"
---

# Source generators (Uno.UI.SourceGenerators)

Generators run inside the compiler — performance and cancellation discipline are load-bearing.

## Architecture map
- **Projects**: `Uno.UI.SourceGenerators` (main XAML + DependencyObject generators), `Uno.UI.SourceGenerators.Internal` (utilities), `Uno.UI.Tasks` (MSBuild tasks incl. `RuntimeAssetsSelectorTask`).
- **XAML pipeline** (`XamlGenerator/`): XAML files → parse (parallel, cached) → build name/maps → generate C# (parallel). Key classes: `XamlCodeGenerator` (entry `ISourceGenerator`), `XamlFileGenerator` (per-file codegen), `XamlFileParser` (parse + checksum cache), `XamlObjectDefinition`/`XamlMemberDefinition` (parsed tree), `NameScope` (named elements, backing fields, components). Emits `InitializeComponent()`, named-element fields, resource-dictionary singletons, `x:Bind` expression methods (`Utils/XBindExpressionParser.cs`), and lazy stubs for `x:Load`.
- **DependencyObject generation** (`DependencyObject/`): emits the `IDependencyObject`/`IDependencyObjectStoreProvider` mixin (`__Store`, `GetValue`/`SetValue`/`ClearValue`) plus platform lifecycle partials — this is why `DependencyObject` can be an *interface* on mobile.

- **New generators are `IIncrementalGenerator`** (fine-grained caching). The large legacy XAML generator is still `ISourceGenerator` (`XamlCodeGenerator`) — match the surrounding file's model when editing it; don't half-convert.
- **Emit via `IndentedStringBuilder`** (from `Uno.Roslyn`), and wrap large output in **`StringBuilderBasedSourceText`** instead of `builder.ToString()` — the latter allocates to the Large Object Heap. Don't mutate the builder after wrapping it.
- **Respect cancellation**: call `context.CancellationToken.ThrowIfCancellationRequested()` at the start of visitor methods and *before* expensive work (parsing, symbol walks) — not after.
- **Scan with `SymbolVisitor`** (`VisitModule → VisitNamespace → VisitNamedType`), not reflection. Filter inside `ProcessType` — the visitor sees every nested type.
- **No LINQ in hot paths.** Cache resolved `INamedTypeSymbol`s in constructor fields (via `GetTypeByMetadataName`, null-checked → skip generation, don't throw) and compare with `SymbolEqualityComparer.Default`.
- **Skip work correctly**: gate on `PlatformHelper.IsValidPlatform(context)` and `IsDesignTime(context)` (don't generate during VS design-time builds).
- **Read MSBuild config** with `context.GetMSBuildPropertyValue(name)` / `GetMSBuildItemsWithAdditionalFiles(...)`; cache in fields. Always null-coalesce — properties can be null.
- **Diagnostics**: static `DiagnosticDescriptor` with stable IDs (`Uno0003`, `UXAML0001`), reported via `context.ReportDiagnostic`. Add `#pragma warning disable RS2008` or CI fails on analyzer-release tracking.
- **Tests** use `CSharpSourceGeneratorVerifier<T>` (legacy) or `CSharpIncrementalSourceGeneratorVerifier<T>` (incremental) with expected `GeneratedSources`. Update goldens when output changes.
- Debug with `Debugger.Launch()` gated by the `UnoUISourceGeneratorDebuggerBreak` MSBuild flag — never commit a bare `Debugger.Break()`. Dump generator state with `XamlSourceGeneratorTracingFolder`.
