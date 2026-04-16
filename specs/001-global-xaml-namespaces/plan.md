# Implementation Plan: Global/Implicit XAML Namespaces

**Branch**: `001-global-xaml-namespaces` | **Date**: 2026-03-04 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-global-xaml-namespaces/spec.md`

## Summary

Add support for global/implicit XAML namespaces to Uno Platform, allowing developers to write XAML without boilerplate `xmlns` declarations. The feature is enabled by default via the Uno.Sdk. The XAML source generator is modified to inject the implicit namespace declarations into the XAML text via `XamlFileParser.InjectImplicitXmlns` before parsing. Developers register custom namespaces to a dedicated global URI via `[assembly: XmlnsDefinition]` attributes.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: Roslyn source generators, System.Xaml, MSBuild
**Storage**: N/A
**Testing**: Runtime tests (`Uno.UI.RuntimeTests`), Unit tests (`Uno.UI.Tests`)
**Target Platform**: All Uno Platform targets (WebAssembly, Skia Desktop, Android, iOS)
**Project Type**: Framework library (source generator + MSBuild SDK)
**Performance Goals**: No measurable regression in XAML compilation time
**Constraints**: Must not break existing XAML files with explicit `xmlns`
**Scale/Scope**: Affects all XAML files in all Uno Platform projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. WinUI API Fidelity | PASS | `XmlnsDefinition` and `XmlnsPrefix` are standard WinUI APIs. Global namespace feature extends XAML tooling, not the API surface. |
| II. Cross-Platform Parity | PASS | Feature works on all Uno Platform targets via the source generator. |
| III. Test-First Quality Gates | PASS | Runtime tests will be added for all user stories. |
| IV. Performance and Resource Discipline | PASS | `XmlnsDefinition` scanning happens once per compilation (cached). No per-frame impact. |
| V. Generated Code Boundaries | PASS | No changes to Generated/ folders. Feature modifies the source generator itself. |
| VI. Backward Compatibility | PASS | Feature is additive. Existing XAML with explicit `xmlns` works unchanged. Opt-out available. |
| VII. WinUI Implementation Alignment | N/A | This is a tooling feature, not a WinUI control behavior. No WinUI C++ reference implementation exists. |

**Post-Phase 1 re-check**: All gates still pass.

## Project Structure

### Documentation (this feature)

```text
specs/001-global-xaml-namespaces/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Data model documentation
├── quickstart.md        # Developer quickstart guide
├── contracts/           # API contracts
│   ├── msbuild-properties.md
│   └── source-generator-api.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (files to modify/create)

```text
src/SourceGenerators/Uno.UI.SourceGenerators/
├── XamlGenerator/
│   ├── XamlConstants.cs                    # ADD: GlobalNamespaceUri constant
│   ├── XamlCodeGeneration.cs               # MODIFY: Read UnoEnableImplicitXamlNamespaces property
│   ├── XamlFileParser.cs                   # MODIFY: Inject implicit namespaces via XmlParserContext
│   ├── XamlFileGenerator.cs                # MODIFY: Pass global namespace config to reflection
│   ├── XamlFileGenerator.Reflection.cs     # MODIFY: Add global namespace type resolution
│   └── GlobalNamespaceResolver.cs          # NEW: Discover XmlnsDefinition/XmlnsPrefix from compilation
├── Content/
│   └── Uno.UI.SourceGenerators.props       # MODIFY: Add CompilerVisibleProperty

src/Uno.Sdk/
├── targets/
│   └── Uno.ImplicitXamlNamespaces.props    # NEW: Set default property values
├── Sdk/
│   └── Sdk.props                           # MODIFY: Import new props

src/Uno.UI.RuntimeTests/
└── Tests/Windows_UI_Xaml/
    └── Given_ImplicitXamlNamespaces.cs     # NEW: Runtime tests
```

**Structure Decision**: This feature spans the source generator (compile-time XAML processing), the Uno.Sdk (MSBuild property defaults), and runtime tests. No new projects are created; changes are distributed across existing projects.

## Design Details

### Component 1: Source Generator - Implicit Namespace Injection

**File**: `XamlFileParser.cs`

When `UnoEnableImplicitXamlNamespaces` is true, the XAML text is rewritten before parsing so the root element carries the required `xmlns` declarations. The parser locates the root element's opening tag and, if missing, inserts:

- `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` (the default WinUI presentation namespace)
- `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"`
- any `xmlns:{prefix}="{uri}"` declarations discovered from `XmlnsPrefix` attributes (skipping reserved prefixes `x`, `xml`, `xmlns`)

The rewrite is implemented by `InjectImplicitXmlns(string content)` and applied via the normal `XmlReader.Create(new StringReader(content))` path — no `XmlParserContext` / `ConformanceLevel.Fragment` is needed. Existing declarations on the root tag are preserved and take precedence.

### Component 2: Source Generator - Global Namespace Type Resolution

**File**: `XamlFileGenerator.Reflection.cs`

The `InitCaches()` method (line 29) populates `_clrNamespaces` from `_knownNamespaces` based on the file's default XML namespace. When no default namespace is declared (implicit mode), `_clrNamespaces` would be empty.

**Fix**: When implicit namespaces are enabled:
1. Always populate `_clrNamespaces` with `PresentationNamespaces` (the implicit default).
2. Add a new `_globalClrNamespaces` array from `GlobalNamespaceResolver`.
3. In `SourceFindTypeByXamlType()`, after searching `_clrNamespaces`, also search `_globalClrNamespaces`.

### Component 3: GlobalNamespaceResolver (new)

**File**: `GlobalNamespaceResolver.cs`

Scans the Roslyn `Compilation` for `XmlnsDefinition` attributes targeting the global URI:

```csharp
internal static class GlobalNamespaceResolver
{
    public static string[] GetGlobalClrNamespaces(Compilation compilation, string globalUri)
    {
        var namespaces = new List<string>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
            {
                foreach (var attr in assembly.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "XmlnsDefinitionAttribute"
                        && attr.ConstructorArguments.Length >= 2
                        && attr.ConstructorArguments[0].Value is string uri
                        && uri == globalUri
                        && attr.ConstructorArguments[1].Value is string clrNs)
                    {
                        namespaces.Add(clrNs);
                    }
                }
            }
        }
        // Also scan the compilation's own assembly
        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            // Same logic for current assembly
        }
        return namespaces.Distinct().ToArray();
    }
}
```

### Component 4: MSBuild Integration

**File**: `Uno.ImplicitXamlNamespaces.props` (new)
```xml
<Project>
  <PropertyGroup>
    <UnoEnableImplicitXamlNamespaces Condition="'$(UnoEnableImplicitXamlNamespaces)'==''">true</UnoEnableImplicitXamlNamespaces>
    <UnoGlobalXamlNamespaceUri Condition="'$(UnoGlobalXamlNamespaceUri)'==''">http://schemas.microsoft.com/winfx/2006/xaml/presentation/global</UnoGlobalXamlNamespaceUri>
  </PropertyGroup>
</Project>
```

**File**: `Uno.UI.SourceGenerators.props` (modify)
```xml
<CompilerVisibleProperty Include="UnoEnableImplicitXamlNamespaces" />
<CompilerVisibleProperty Include="UnoGlobalXamlNamespaceUri" />
```

### Component 5: Ambiguity Error Handling

When `SourceFindTypeByXamlType` finds a type name in multiple global CLR namespaces, emit a diagnostic:

```
UNO0501: Ambiguous type reference 'Card'. Found in:
  - Uno.Toolkit.Controls (registered via XmlnsDefinition)
  - MyApp.Controls (registered via XmlnsDefinition)
  Use an explicit xmlns prefix to disambiguate.
```

## Complexity Tracking

No constitution violations to justify. All changes follow existing patterns.
