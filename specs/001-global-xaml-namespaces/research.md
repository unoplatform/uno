# Research: Global/Implicit XAML Namespaces

**Feature**: `001-global-xaml-namespaces`
**Date**: 2026-03-04

## Decision 1: XAML Parsing Approach for Implicit Namespaces

**Decision**: Use `XmlParserContext` with pre-populated `XmlNamespaceManager` and `ConformanceLevel.Fragment`, matching MAUI's proven approach.

**Rationale**: Uno's `XamlFileParser.cs` creates an `XmlReader` via `XmlReader.Create(new StringReader(xaml))` at line 182. By providing an `XmlParserContext` with pre-configured namespace mappings and switching to `ConformanceLevel.Fragment`, the XML parser can handle XAML without explicit `xmlns` declarations. This is identical to how MAUI implements it in their `XamlLoader.cs`, `XamlCTask.cs`, and source generator - a pattern proven in production.

**Alternatives considered**:
- **String rewriting**: Inject `xmlns` declarations into XAML text before parsing. Rejected because it modifies source positions (breaking error reporting and line info), adds complexity, and is fragile with edge cases.
- **Custom XML reader wrapper**: Intercept namespace resolution at the reader level. Rejected because it's more complex and the `XmlParserContext` approach already solves the problem cleanly.

## Decision 2: WinAppSDK Target Strategy

**Decision**: MSBuild pre-processing task that generates temporary XAML copies with injected `xmlns` declarations before the WinUI `XamlCompiler.exe` runs.

**Rationale**: The Uno source generator is **disabled** for WinAppSDK targets (`ShouldRunGenerator=false` in `Uno.UI.SourceGenerators.props` line 10-11). The WinUI XAML compiler (`XamlCompiler.exe`) is closed-source and cannot be modified. Therefore, XAML files must have explicit `xmlns` declarations when they reach the WinUI compiler. An MSBuild task that runs before `XamlPreCompile` can generate temp copies with injected namespaces while preserving the original files.

**Note**: MAUI does NOT need this approach because MAUI has its own XAML compiler (`XamlCTask`). Uno must bridge the gap for the WinUI compiler.

**Alternatives considered**:
- **MAUI-style attribute approach**: Not feasible because WinUI's `XamlCompiler.exe` doesn't recognize `AllowImplicitXmlnsDeclarationAttribute`.
- **Skip WinAppSDK**: Rejected per spec requirement FR-012 and clarification session.

## Decision 3: Feature Flag Mechanism

**Decision**: MSBuild property `UnoEnableImplicitXamlNamespaces` (default `true`), passed to source generator via `CompilerVisibleProperty`.

**Rationale**: The Uno source generator already receives MSBuild properties via `CompilerVisibleProperty` items defined in `Uno.UI.SourceGenerators.props`. Adding a new property follows the established pattern. Default `true` per clarification (enabled for all projects via Uno.Sdk with opt-out).

**Alternatives considered**:
- **Assembly-level attribute** (MAUI approach): Rejected because it requires compilation to take effect, creating a chicken-and-egg problem with the source generator which runs during compilation. MSBuild property is available before compilation starts.
- **Define constant** (`#define`): Rejected because it requires project file modification and is less discoverable than a property.

## Decision 4: Global Namespace URI

**Decision**: `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global` - a dedicated URI separate from the presentation namespace.

**Rationale**: Per clarification, user-registered types should be isolated from the framework's default namespace. Using a suffix of the existing presentation URI maintains consistency with WinUI conventions while clearly separating user types.

**Alternatives considered**:
- **Reuse presentation URI**: Rejected per clarification - would mix user types with framework types.
- **MAUI's URI** (`http://schemas.microsoft.com/dotnet/maui/global`): Rejected because it's MAUI-specific and could cause unintended cross-contamination.

## Decision 5: XmlnsDefinition Attribute Discovery

**Decision**: Scan `XmlnsDefinition` attributes from all referenced assemblies at source generator time using Roslyn's `Compilation.Assembly` and `Compilation.References`.

**Rationale**: The source generator has access to the full Roslyn compilation, which includes metadata for all referenced assemblies. By scanning for `XmlnsDefinition` attributes targeting the global URI, we can dynamically discover all CLR namespaces that should be searchable for unprefixed type resolution. This mirrors how MAUI's `GatherXmlnsDefinitionAndXmlnsPrefixAttributes` works at runtime, but adapted for compile-time.

**Key implementation detail**: The existing `_knownNamespaces` dictionary in `XamlFileGenerator.cs` (line 39) is static and hardcoded. We need to augment it dynamically with namespaces discovered from `XmlnsDefinition` attributes targeting the global URI.

## Decision 6: Type Resolution Priority

**Decision**: When resolving unprefixed types, search order is:
1. Presentation namespace CLR namespaces (existing `_knownNamespaces` for the default xmlns)
2. Global namespace CLR namespaces (from `XmlnsDefinition` attributes targeting the global URI)
3. Standard fallbacks (using:, clr-namespace:)

**Rationale**: WinUI framework types should always take precedence over user-registered types to prevent accidental shadowing of framework controls.

## Key Files to Modify

| File | Change |
|------|--------|
| `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileParser.cs` | Inject implicit namespaces via `XmlParserContext` when feature enabled |
| `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.Reflection.cs` | Add global namespace resolution to type lookup |
| `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlConstants.cs` | Add `GlobalNamespaceUri` constant |
| `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs` | Read new MSBuild property, pass to parser |
| `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props` | Add `CompilerVisibleProperty` for feature flag |
| `src/Uno.Sdk/targets/` | Add feature property default, WinAppSDK pre-processing task |
| `src/Uno.UI.RuntimeTests/` | Runtime tests for all scenarios |

## MAUI Implementation Reference

MAUI's approach (studied from `D:\Work\maui`):
- `AllowImplicitXmlnsDeclarationAttribute` - assembly-level opt-in
- `XamlParser.Namespaces.cs` - defines `MauiGlobalUri`
- `XamlLoader.cs` - runtime namespace injection via `XmlNamespaceManager`
- `XamlCTask.cs` - compile-time namespace injection (same pattern)
- `XamlGenerator.cs` - source generator emits the attribute
- `ConformanceLevel.Fragment` - allows XML without root xmlns

Key difference for Uno: MAUI controls both the XAML compiler and runtime. Uno must also handle the WinUI XAML compiler (WinAppSDK), which requires the MSBuild pre-processing approach.
