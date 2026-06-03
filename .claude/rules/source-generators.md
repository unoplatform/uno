---
description: Roslyn source-generator conventions and performance constraints for Uno.UI.SourceGenerators. Auto-loaded when editing generators. Full reference in .github/agents/source-generators-agent.md.
paths:
  - "src/SourceGenerators/**/*.cs"
---

# Source generators (Uno.UI.SourceGenerators)

Deep reference: `.github/agents/source-generators-agent.md`. Generators run inside the compiler — performance and cancellation discipline are load-bearing.

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
