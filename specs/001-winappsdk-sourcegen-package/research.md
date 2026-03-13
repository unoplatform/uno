# Research: WinAppSDK Source Generator Package

**Date**: 2026-03-13
**Feature**: 001-winappsdk-sourcegen-package

## Decision 1: Code Sharing Strategy

**Decision**: Copy the 4 code-behind source files into the new project rather than using a shared project or source linking.

**Rationale**:
- The code-behind generator files are small, self-contained, and have minimal dependencies:
  - `XamlCodeBehindGenerator.cs` (~135 lines) — the incremental generator
  - `XamlCodeBehindParser.cs` (~152 lines) — lightweight XML parser
  - `XamlCodeBehindEmitter.cs` (~38 lines) — source text emitter
  - `XamlClassInfo.cs` (~14 lines) — record struct for parsed XAML metadata
- Only external dependency is `XamlCodeGenerationDiagnostics.InvalidXClassRule` — a single `DiagnosticDescriptor` that can be trivially defined in the new project
- A shared project would couple the two packages unnecessarily and complicate the build
- The total copied code is ~340 lines — well within acceptable duplication for full isolation

**Alternatives considered**:
- **Shared project (`.shproj`)**: Rejected — adds build complexity and couples packages that should be independent
- **Source linking via `<Compile Include="..\..\">` paths**: Rejected — fragile, creates implicit coupling
- **NuGet package dependency**: Rejected — source generators cannot depend on other NuGet packages at analyzer load time

## Decision 2: Gate Removal from Uno.UI.SourceGenerators

**Decision**: Remove the `UnoCodeBehindGeneratorOnly` gate and the `XamlCodeBehindGenerator` from `Uno.UI.SourceGenerators` entirely.

**Rationale**:
- The standalone `XamlCodeBehindGenerator` in `Uno.UI.SourceGenerators` exists solely for WinAppSDK targets
- On Uno Platform targets, code-behind generation is handled by the integrated `XamlCodeGeneration` pipeline
- Once the new package exists, there is no need for the standalone generator in the original assembly
- Removing it eliminates the `UnoCodeBehindGeneratorOnly` gate mechanism entirely

**Alternatives considered**:
- **Keep both**: Rejected — defeats the purpose; the original would never run
- **Keep as fallback**: Rejected — unnecessary complexity; the new package is the canonical path

## Decision 3: Diagnostic Descriptor Handling

**Decision**: Define the `InvalidXClassRule` diagnostic descriptor directly in the new package rather than referencing `XamlCodeGenerationDiagnostics`.

**Rationale**:
- `XamlCodeGenerationDiagnostics` is a static class within `Uno.UI.SourceGenerators` that contains diagnostics for many generators
- The new package only needs one descriptor (`UNOB0001` / `InvalidXClassRule`)
- Copying a single `DiagnosticDescriptor` definition is cleaner than taking a dependency on the entire diagnostics class
- The diagnostic ID (`UNOB0001`) and message remain identical for consistency

**Alternatives considered**:
- **Reference the existing class**: Not possible — different assembly
- **Shared project for diagnostics**: Overkill for a single descriptor

## Decision 4: Package Versioning

**Decision**: Follow the existing Uno package versioning scheme — the new package version matches other Uno packages.

**Rationale**:
- All Uno packages are versioned together (e.g., `Uno.WinUI`, `Uno.UI.Adapter.Microsoft.Extensions.Logging`, etc.)
- Using the same version ensures compatibility and simplifies release management
- The `UnoNugetOverrideVersion` property should apply to the new package as well

**Alternatives considered**:
- **Independent versioning**: Rejected — creates confusion about which versions are compatible

## Decision 5: MSBuild Target Simplification

**Decision**: Eliminate the remove-and-re-add analyzer workaround entirely. The new package is referenced directly for WinAppSDK targets.

**Rationale**:
- Currently: `_RemoveRoslynUnoSourceGenerationWinUI` removes `Uno.UI.SourceGenerators`, then `_AddCodeBehindGeneratorForWinUI` re-adds it
- With the new package: `Uno.UI.SourceGenerators` is simply not referenced for WinAppSDK targets; the new package is referenced instead
- This eliminates three MSBuild targets (`_RemoveRoslynUnoSourceGenerationWinUI`, `_AddCodeBehindGeneratorForWinUI`, `_InjectAdditionalFilesForWinUI`) and replaces them with a straightforward package reference
- The new package ships its own `.props` file that configures `CompilerVisibleProperty` and `AdditionalFiles` injection

**Alternatives considered**:
- **Keep workaround targets**: Rejected — the whole point is to eliminate this fragile pattern

## Decision 6: NuGet Package Structure

**Decision**: Create the new package as a separate `.nuspec` file in `build/nuget/` following the existing pattern.

**Rationale**:
- The existing `Uno.WinUI.nuspec` packages `Uno.UI.SourceGenerators.dll` in `analyzers/dotnet/cs/`
- The new package will have its own nuspec with the new DLL in `analyzers/dotnet/cs/`
- The package does NOT need `Uno.Xaml.dll` or telemetry dependencies (the code-behind generator uses only `System.Xml.Linq` from the BCL)
- The `.props` file goes in `buildTransitive/` for automatic import

**Alternatives considered**:
- **Embed in Uno.WinUI.nuspec**: Rejected — the new DLL is a separate assembly; mixing would be confusing
- **Pack via .csproj `<GeneratePackageOnBuild>`**: Could work but does not match existing conventions in this repo

## Decision 7: Test Strategy

**Decision**: Existing tests remain in `Uno.UI.SourceGenerators.Tests` but reference the generator type from the new project. Alternatively, a new test file can be added that references the new assembly.

**Rationale**:
- The existing `XamlCodeBehindGeneratorTests` uses `CSharpIncrementalSourceGeneratorVerifier<XamlCodeBehindGenerator>` which requires a reference to the generator type
- Since the generator will move to the new assembly, the test project needs a reference to the new project
- The test infrastructure and patterns remain the same

**Alternatives considered**:
- **Separate test project**: Unnecessary overhead for the same test patterns
- **Duplicate tests**: Rejected — test maintenance burden
