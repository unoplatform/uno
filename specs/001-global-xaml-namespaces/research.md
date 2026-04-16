# Research: Global/Implicit XAML Namespaces

**Feature**: `001-global-xaml-namespaces`
**Date**: 2026-03-04

## Decision 1: XAML Parsing Approach for Implicit Namespaces

**Decision**: Inject missing `xmlns` declarations into the root element of each XAML file before parsing (string rewriting via `InjectImplicitXmlns`).

**Rationale**: The initial plan was to use `XmlParserContext` with `ConformanceLevel.Fragment` (MAUI's approach), but this was changed during implementation because `ConformanceLevel.Fragment` alters `XmlReader` behavior in ways that broke Uno's existing XAML parser assumptions (e.g., error positions, node handling). Instead, the source generator's `XamlFileParser.InjectImplicitXmlns` method scans for the root element's opening tag and inserts any missing `xmlns` declarations (default, `xmlns:x`, and implicit `XmlnsPrefix` prefixes) before the closing `>`. This preserves the existing `XmlReader` pipeline unchanged while making the XAML valid XML before it reaches the parser.

**Alternatives considered**:
- **`XmlParserContext` + `ConformanceLevel.Fragment`** (MAUI approach): Initially chosen, but rejected during implementation because `ConformanceLevel.Fragment` changes `XmlReader` behavior in ways incompatible with Uno's parser pipeline.
- **Custom XML reader wrapper**: Intercept namespace resolution at the reader level. Rejected because it's more complex and fragile.

## Decision 2: Feature Flag Mechanism

**Decision**: MSBuild property `UnoEnableImplicitXamlNamespaces` (default `true`), passed to source generator via `CompilerVisibleProperty`.

**Rationale**: The Uno source generator already receives MSBuild properties via `CompilerVisibleProperty` items defined in `Uno.UI.SourceGenerators.props`. Adding a new property follows the established pattern. Default `true` per clarification (enabled for all projects via Uno.Sdk with opt-out).

**Alternatives considered**:
- **Assembly-level attribute** (MAUI approach): Rejected because it requires compilation to take effect, creating a chicken-and-egg problem with the source generator which runs during compilation. MSBuild property is available before compilation starts.
- **Define constant** (`#define`): Rejected because it requires project file modification and is less discoverable than a property.

## Decision 3: Global Namespace URI

**Decision**: `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global` - a dedicated URI separate from the presentation namespace.

**Rationale**: Per clarification, user-registered types should be isolated from the framework's default namespace. Using a suffix of the existing presentation URI maintains consistency with WinUI conventions while clearly separating user types.

**Alternatives considered**:
- **Reuse presentation URI**: Rejected per clarification - would mix user types with framework types.
- **MAUI's URI** (`http://schemas.microsoft.com/dotnet/maui/global`): Rejected because it's MAUI-specific and could cause unintended cross-contamination.

## Decision 4: XmlnsDefinition Attribute Discovery

**Decision**: Scan `XmlnsDefinition` attributes from all referenced assemblies at source generator time using Roslyn's `Compilation.Assembly` and `Compilation.References`.

**Rationale**: The source generator has access to the full Roslyn compilation, which includes metadata for all referenced assemblies. By scanning for `XmlnsDefinition` attributes targeting the global URI, we can dynamically discover all CLR namespaces that should be searchable for unprefixed type resolution. This mirrors how MAUI's `GatherXmlnsDefinitionAndXmlnsPrefixAttributes` works at runtime, but adapted for compile-time.

**Key implementation detail**: The existing `_knownNamespaces` dictionary in `XamlFileGenerator.cs` (line 39) is static and hardcoded. We need to augment it dynamically with namespaces discovered from `XmlnsDefinition` attributes targeting the global URI.

## Decision 5: Type Resolution Priority

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
| `src/Uno.Sdk/targets/` | Add feature property defaults |
| `src/Uno.UI.RuntimeTests/` | Runtime tests for all scenarios |

## MAUI Implementation Reference

MAUI's approach (studied from `D:\Work\maui`):
- `AllowImplicitXmlnsDeclarationAttribute` - assembly-level opt-in
- `XamlParser.Namespaces.cs` - defines `MauiGlobalUri`
- `XamlLoader.cs` - runtime namespace injection via `XmlNamespaceManager`
- `XamlCTask.cs` - compile-time namespace injection (same pattern)
- `XamlGenerator.cs` - source generator emits the attribute
- `ConformanceLevel.Fragment` - allows XML without root xmlns

Key difference: MAUI controls both the XAML compiler and runtime. Uno uses the same `XmlParserContext` approach in its source generator.
