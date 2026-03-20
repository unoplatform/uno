# Implementation Plan: Auto-Generate Code-Behind for XAML Files

**Branch**: `001-auto-codebehind` | **Date**: 2026-03-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-auto-codebehind/spec.md`

## Summary

Auto-generate a minimal code-behind partial class (constructor + `InitializeComponent()`) for XAML files with `x:Class` when no developer-authored class exists in the compilation. The feature is controlled by a project-level MSBuild property (`UnoGenerateCodeBehind`, default `true`) and per-file item metadata (`GenerateCodeBehind`). It must work on both Uno Platform targets and WinUI targets via Uno.Sdk.

**Two-pronged approach** due to generator interaction constraints:
- **Uno Platform targets**: Integrate code-behind generation directly into the existing `XamlCodeGenerator` pipeline (same compilation pass, avoiding Symbol-null issues)
- **WinUI targets**: Use a separate lightweight `IIncrementalGenerator` (no interaction concern since Uno's XAML generator is disabled on WinUI; WinUI's own XAML compiler handles `InitializeComponent()`)

## Technical Context

**Language/Version**: C# / .NET Standard 2.0 (source generator assembly), targeting .NET 10.0 projects
**Primary Dependencies**: Microsoft.CodeAnalysis (Roslyn APIs), Uno.UI.SourceGenerators infrastructure
**Storage**: N/A (build-time source generation only)
**Testing**: MSBuild-based source generator tests (`Uno.UI.SourceGenerators.Tests`), runtime tests (`Uno.UI.RuntimeTests`)
**Target Platform**: All Uno Platform targets (Skia, WebAssembly, Android, iOS, macOS, tvOS) + WinUI (`net10.0-windows10.0.*`)
**Project Type**: Library (source generator DLL) + MSBuild integration (.props/.targets)
**Performance Goals**: Incremental generation - no re-generation when inputs haven't changed; sub-second for typical projects
**Constraints**: Must be `netstandard2.0` (Roslyn generator requirement); must not interfere with existing XAML generation pipeline
**Scale/Scope**: Typical projects have 10-50 XAML files; large projects may have 200+

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. WinUI API Fidelity | PASS | No new public API surfaces; code-behind generation is a build-time convenience. WinUI itself uses code-behind; this just auto-generates the boilerplate. |
| II. Cross-Platform Parity | PASS | FR-010/FR-011 require all platforms. Generator produces identical output for all targets. WinUI support via separate mechanism since existing generators are disabled there. |
| III. Test-First Quality Gates | PASS | Will add source generator unit tests and runtime tests for Skia. |
| IV. Performance and Resource Discipline | PASS | Uno targets: integrated into existing generator pass (no extra overhead). WinUI targets: lightweight `IIncrementalGenerator` with incremental caching. No hot-path impact (build-time only). |
| V. Generated Code Boundaries | PASS | New generator produces source-generated files (not in `Generated/` folders which are for API stubs). Follows the same pattern as existing XAML source generation. |
| VI. Backward Compatibility | PASS | Feature is additive. Default `true` is safe because existing projects already have code-behind files, so the generator will detect the existing class via `Compilation.GetTypeByMetadataName()` and skip generation. No behavioral change for existing projects (SC-006). |
| VII. WinUI Implementation Alignment | N/A | This is an Uno-specific build tooling feature; no WinUI C++ equivalent exists. |

**Gate Result**: PASS - No violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-auto-codebehind/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/SourceGenerators/Uno.UI.SourceGenerators/
├── XamlGenerator/
│   ├── XamlCodeGeneration.cs               # Modified: code-behind generation integrated here
│   ├── XamlCodeBehindGenerator.cs          # New IIncrementalGenerator (WinUI-only path)
│   └── XamlFileGenerator.cs               # Modified: suppress #warning when code-behind is auto-generated
├── Content/
│   └── Uno.UI.SourceGenerators.props       # Modified: add CompilerVisibleProperty/Metadata

src/SourceGenerators/Uno.UI.SourceGenerators.Tests/
└── XamlCodeBehindGeneratorTests/
    └── XamlCodeBehindGeneratorTests.cs     # Unit tests for the generator

src/Uno.UI.RuntimeTests/
└── Tests/Windows_UI_Xaml/
    └── Given_AutoCodeBehind.cs             # Runtime tests

src/Uno.Sdk/targets/
└── Uno.Common.WinAppSdk.targets             # Modified: imports WinAppSdk.props for code-behind
```

**Structure Decision**: Two integration paths in the same assembly:
1. **Uno targets**: Code-behind generation is integrated directly into the existing `XamlCodeGeneration.Generate()` pipeline. When `Symbol` is null and generation is enabled, the pipeline emits an additional source file with the code-behind class, and the existing `XamlFileGenerator` is informed that the class is being auto-generated (suppressing the `#warning` and enabling proper x:Bind support).
2. **WinUI targets**: A separate `IIncrementalGenerator` (`XamlCodeBehindGenerator`) handles code-behind generation independently. This is safe because on WinUI targets, Uno's XAML generator is completely disabled (`ShouldRunGenerator=false`), so there is no interaction concern. WinUI's own XAML compiler generates `InitializeComponent()`.

## Key Architecture Decisions

### 1. Why Not a Separate IIncrementalGenerator for Uno Targets (Critical)

**Problem**: Roslyn source generators in the same compilation all see the **original** compilation — they cannot see each other's output in the same pass. If the code-behind generator and XAML generator were separate generators:

1. Code-behind generator checks `GetTypeByMetadataName("MyApp.MainPage")` → null → emits partial class
2. XAML generator checks `FindTypeByFullName("MyApp.MainPage")` → **also null** (same original compilation) → sees `Symbol=null`

When `_xClassName.Symbol` is null, the existing XAML generator:
- **Emits a `#warning`** about inconsistent base type (`TryGenerateWarningForInconsistentBaseType`)
- **Hard-fails on `x:Bind`** expressions (`GetXBindPropertyPathType` throws `XamlGenerationException`)
- **Hard-fails on x:Bind event bindings** (`GetTargetType()` throws `XamlGenerationException`)
- Still generates `InitializeComponent()` and the partial class declaration (base type from XAML root element)

This means a separate generator would cause spurious warnings and break any XAML using `x:Bind` — unacceptable.

**Solution**: Integrate code-behind generation directly into the existing `XamlCodeGeneration` pipeline for Uno targets. This way the pipeline knows it is auto-generating the code-behind and can:
- Skip the `#warning` for auto-generated code-behind files
- Provide a synthetic `INamedTypeSymbol` or flag to `XamlFileGenerator` so `x:Bind` works correctly

### 2. Two-Path Architecture

| Path | Targets | Mechanism | Why |
|------|---------|-----------|-----|
| **Integrated** | All Uno Platform targets (Skia, WASM, Mobile) | Extension to `XamlCodeGeneration.Generate()` | Avoids Symbol-null interaction with XAML generator |
| **Standalone** | WinUI (`net10.0-windows10.0.*`) | Separate `IIncrementalGenerator` | Safe: Uno's XAML generator is disabled on WinUI (`ShouldRunGenerator=false`). WinUI's own XAML compiler handles `InitializeComponent()`. No interaction concern. |

### 3. Integrated Path: How It Works (Uno Targets)

Inside `XamlCodeGeneration.Generate()`, after collecting XAML files and before calling `XamlFileGenerator.GenerateFile()`:

1. For each XAML file with `x:Class` where `FindClassSymbol()` returns null:
   a. Check `UnoGenerateCodeBehind` property and per-file `GenerateCodeBehind` metadata
   b. If generation is enabled, emit a separate source file with the code-behind class
   c. Mark the file internally as "auto-generated code-behind" so `XamlFileGenerator` knows to:
      - Suppress the `#warning` in `TryGenerateWarningForInconsistentBaseType`
      - Use the XAML-derived base type as the authoritative type for `x:Bind` resolution (instead of requiring `_xClassName.Symbol`)

The XAML generator's existing partial class declaration (`partial class MyPage : Page`) already uses the XAML root element type, not the Symbol. So the code-behind generator's `partial class MyPage : Page` is compatible — both declare the same base type.

### 4. Standalone Path: How It Works (WinUI Targets)

A separate `IIncrementalGenerator` (`XamlCodeBehindGenerator`) in the same assembly:
- Uses `IIncrementalGenerator` (modern API, incremental caching)
- Reads `AdditionalFiles` with `SourceItemGroup` metadata to find XAML files
- Parses `x:Class` and root element via lightweight XML (`XDocument`)
- Checks `Compilation.GetTypeByMetadataName()` to detect missing classes
- Emits the partial class with constructor

This generator is **only active on WinUI targets** (gated by a build property check). On Uno targets, it does nothing (the integrated path handles it).

### 5. Detection Strategy: Compilation Symbol Check

Both paths check `Compilation.GetTypeByMetadataName(xClassFullName)` to determine if the class exists. This is the same approach used by `XamlFileGenerator.FindClassName()`. Benefits:
- Works regardless of file naming conventions
- Detects classes defined anywhere in the compilation (not just conventional `.xaml.cs` files)
- No file-system access needed (pure Roslyn compilation analysis)

### 6. XAML Root Element Type Resolution

The generated class must inherit from the correct base type (e.g., `Page`, `UserControl`, `Window`).

- **Integrated path (Uno targets)**: Reuses the existing type resolution from `XamlCodeGeneration` — the `GetType(topLevelControl.Type)` call already resolves the XAML root element to a fully-qualified type symbol.
- **Standalone path (WinUI targets)**: Uses lightweight XML parsing to extract the root element name and a hardcoded map of common WinUI types:
  - `Page` → `Microsoft.UI.Xaml.Controls.Page`
  - `UserControl` → `Microsoft.UI.Xaml.Controls.UserControl`
  - `Window` → `Microsoft.UI.Xaml.Window`
  - `ResourceDictionary` → `Microsoft.UI.Xaml.ResourceDictionary`
  - `ContentDialog` → `Microsoft.UI.Xaml.Controls.ContentDialog`
  - `Application` → `Microsoft.UI.Xaml.Application`

### 7. XamlFileGenerator Modifications for Symbol-Null Handling

When the integrated path marks a file as "auto-generated code-behind", `XamlFileGenerator` needs these adjustments:

| Location | Current behavior (Symbol=null) | New behavior (auto-generated) |
|----------|-------------------------------|-------------------------------|
| `TryGenerateWarningForInconsistentBaseType` | Emits `#warning` | Suppressed (base type is known from XAML root) |
| `XBindExpressionParser.Rewrite` | Skips nullability rewriter, emits untyped binding | Use XAML-derived type for type-safe resolution |
| `GetXBindPropertyPathType` | Throws `XamlGenerationException` | Use XAML-derived base type as root type |
| `GetTargetType` for x:Bind events | Throws `XamlGenerationException` | Use XAML-derived base type |
| `_xClassName.ToString()` in Bindings | Returns `"Namespace.ClassName"` (no `global::`) | Same (acceptable; compiles correctly) |

The simplest implementation: when the code-behind is auto-generated, create a synthetic `INamedTypeSymbol` or pass the resolved base type symbol as `_xClassName.Symbol` substitute to the affected code paths.

### 8. MSBuild Property & Metadata Design

```xml
<!-- Project-level toggle (in .csproj) -->
<PropertyGroup>
  <UnoGenerateCodeBehind>true</UnoGenerateCodeBehind>  <!-- default: true -->
</PropertyGroup>

<!-- Per-file override (in .csproj) -->
<ItemGroup>
  <Page Update="Special.xaml">
    <UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>
  </Page>
</ItemGroup>
```

Flow to generator:
- `<CompilerVisibleProperty Include="UnoGenerateCodeBehind" />` → `build_property.UnoGenerateCodeBehind`
- `<CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="UnoGenerateCodeBehind" />` → `build_metadata.AdditionalFiles.GenerateCodeBehind`

## Complexity Tracking

No constitution violations to justify.
