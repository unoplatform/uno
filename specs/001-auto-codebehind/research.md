# Research: Auto-Generate Code-Behind for XAML Files

**Feature**: 001-auto-codebehind | **Date**: 2026-03-05

## Research Topics

### 1. How to detect missing code-behind in a Roslyn source generator

**Decision**: Use `Compilation.GetTypeByMetadataName(xClassFullName)` to check if the class exists.

**Rationale**: This is the exact same mechanism used by the existing `XamlFileGenerator.FindClassName()` (at `XamlFileGenerator.cs:2133`). When `GetTypeByMetadataName()` returns `null`, the class does not exist in the compilation. This covers:
- Developer-authored `.xaml.cs` files
- Classes defined in any other source file (non-conventional naming)
- Classes from referenced assemblies

**Alternatives considered**:
- **File-system check** (`*.xaml.cs` exists): Rejected. Source generators shouldn't rely on file-system state; they work with the Roslyn compilation model. Would also miss classes defined in non-conventionally-named files.
- **AdditionalFiles metadata check** (look for matching `.cs` in `Compile` items): Rejected. MSBuild items aren't directly available; only `AdditionalFiles` and `CompilerVisibleProperty`/`CompilerVisibleItemMetadata` are accessible. Also doesn't account for classes defined elsewhere.

### 2. Separate IIncrementalGenerator vs Integrated into XamlCodeGenerator

**Decision**: Two-path approach:
- **Uno targets**: Integrate into the existing `XamlCodeGeneration.Generate()` pipeline
- **WinUI targets**: Separate `IIncrementalGenerator` (standalone)

**Rationale**:
Roslyn source generators in the same compilation all see the **original** compilation — they cannot see each other's output in the same pass. If a separate generator emitted the code-behind class, the existing XAML generator would still see `Symbol=null` because it runs against the same original compilation. This causes:

1. `TryGenerateWarningForInconsistentBaseType` emits a `#warning` (line 262-285 of `XamlFileGenerator.cs`)
2. `GetXBindPropertyPathType` throws `XamlGenerationException` for TwoWay x:Bind (line 4419-4421)
3. `GetTargetType()` throws `XamlGenerationException` for x:Bind event bindings (line 3620-3627)
4. `XBindExpressionParser.Rewrite` skips nullability checking (emits unsafe untyped binding)

By integrating into `XamlCodeGeneration`, the pipeline knows it is auto-generating the code-behind and can inform `XamlFileGenerator` to suppress the warning and use the XAML-derived base type for x:Bind resolution.

On WinUI targets, a separate `IIncrementalGenerator` is safe because:
- Uno's XAML generator is completely disabled (`ShouldRunGenerator=false`, `_RemoveRoslynUnoSourceGeneration` removes it from Analyzers)
- WinUI's own XAML compiler handles `InitializeComponent()` generation
- No interaction between generators exists

**Alternatives considered**:
- **Separate `IIncrementalGenerator` for all targets**: Rejected. Causes Symbol-null issues with the existing XAML generator on Uno targets (see above).
- **Integrate into `XamlCodeGenerator` for all targets including WinUI**: Not possible. The entire `XamlCodeGenerator` is disabled on WinUI targets via `ShouldRunGenerator=false` and `PlatformHelper.IsValidPlatform()`.
- **Modify Roslyn generator ordering**: Not possible. Roslyn does not support inter-generator ordering or visibility within a single compilation pass.

### 3. How to extract x:Class and root element from XAML in a source generator

**Decision**: Use lightweight XML parsing via `XDocument`/`XElement` on the `AdditionalText.GetText()` content.

**Rationale**:
- The existing `XamlFileParser` is tightly coupled to `XamlCodeGeneration` and uses the full Uno XAML object model. Reusing it would create unnecessary coupling.
- For code-behind generation, we only need two pieces of data: the `x:Class` attribute value and the root element name.
- `XDocument` is available in `netstandard2.0` and is fast for extracting root element attributes.
- Incremental generators need the parse step to be deterministic and cacheable; a simple XML read is ideal.

**Alternatives considered**:
- **Reuse `XamlFileParser`**: Rejected. Too heavyweight; parses the full XAML tree into an object model. Would create tight coupling between the new simple generator and the complex XAML pipeline.
- **Regex extraction**: Rejected. Fragile for XML; doesn't handle namespaces, comments, or encoding correctly.

### 4. How to make the generator work on WinUI targets

**Decision**: Keep the standalone `IIncrementalGenerator` in `Uno.UI.SourceGenerators.dll` but re-add the analyzer DLL on WinUI targets specifically for code-behind generation.

**Rationale**:
- On WinUI targets, `ShouldRunGenerator=false` causes `_RemoveRoslynUnoSourceGeneration` to remove `Uno.UI.SourceGenerators` from `Analyzer` items.
- The code-behind generator needs to run on WinUI targets too (FR-011).
- Solution: A new `.targets` file in Uno.Sdk re-adds the analyzer DLL for WinUI builds when `UnoGenerateCodeBehind=true`.
- The standalone `XamlCodeBehindGenerator` checks a build property (e.g., `build_property.UnoCodeBehindGeneratorOnly`) to know it should only do code-behind generation, not full XAML generation.
- `PlatformHelper.IsValidPlatform()` is bypassed for this generator since it doesn't need the full XAML pipeline.

**Alternatives considered**:
- **Separate NuGet package**: Rejected. Would require a new project, new NuGet infra, and coordination between two packages. Overkill for a small generator.
- **MSBuild-only generation (no Roslyn)**: Rejected. MSBuild targets run before compilation and can't check `Compilation.GetTypeByMetadataName()`. Would have to rely on file-system checks, which is less reliable.

### 5. Generator interaction with existing XAML generator

**Decision**: Eliminate the interaction problem by integrating code-behind generation into the existing XAML generator pipeline on Uno targets.

**Rationale**:
Deep analysis of `XamlFileGenerator.cs` revealed that `Symbol=null` is NOT handled gracefully in all code paths:
- `TryGenerateWarningForInconsistentBaseType` (line 262): Emits `#warning` because `classDefinedBaseType` (null) doesn't match `xamlDefinedBaseType` (Page)
- `GetXBindPropertyPathType` (line 4419): Throws `XamlGenerationException` when `_xClassName?.Symbol` is null and no `rootType` is provided
- `GetTargetType` for x:Bind events (line 3620): Throws `XamlGenerationException` when Symbol is null
- `XBindExpressionParser.Rewrite` (line 4330): Skips nullability rewriter, producing untyped unsafe bindings

A separate generator's output is invisible to the XAML generator in the same pass (Roslyn limitation), so these problems would persist. Integration eliminates them because the pipeline can flag auto-generated files and adjust behavior.

**Alternatives considered**:
- **Separate generator + accept the warnings/limitations**: Rejected. x:Bind hard-failure is a deal-breaker. XAML pages without code-behind should still support x:Bind.
- **Patch XamlFileGenerator to be more null-tolerant**: Possible but fragile. Would require modifying many code paths and wouldn't solve the fundamental problem that the Symbol is needed for type-safe x:Bind resolution.

### 6. XAML root element to base type mapping

**Decision**: Use a hardcoded map of common WinUI root element names to fully-qualified type names, with a fallback to `Microsoft.UI.Xaml.Controls.Page` for unknown types.

**Rationale**:
- The set of common XAML root elements is small and stable (Page, UserControl, Window, ContentDialog, ResourceDictionary, Application).
- The XAML namespace (`xmlns`) on the root element tells us whether it's a default WinUI namespace type or a custom type.
- For custom root elements (e.g., `local:MyBasePage`), the generator can resolve the type from the xmlns prefix mapping + compilation lookup.
- A simple approach covers 99% of cases; edge cases can be addressed later.

**Alternatives considered**:
- **Full XAML namespace resolution**: More accurate but significantly more complex. The existing XAML generator does this, but it's overkill for just finding the base type.
- **No base type (just `partial class`)**: Rejected. The class must inherit from the correct base type so that `InitializeComponent()` and XAML features work correctly. The XAML generator's partial class doesn't declare the base type - it assumes the developer's code-behind does.

### 7. Default value for UnoGenerateCodeBehind

**Decision**: Default to `true` (enabled).

**Rationale**:
- The feature is safe to enable by default because it only generates code-behind for XAML files where the class doesn't already exist in the compilation.
- Existing projects with code-behind files for all XAML pages will see zero behavioral change (SC-006).
- New projects benefit immediately without configuration.
- Aligns with the spec (FR-007).

**Alternatives considered**:
- **Default `false` (opt-in)**: Rejected. Would reduce discoverability and require explicit opt-in, defeating the "zero boilerplate" goal.
