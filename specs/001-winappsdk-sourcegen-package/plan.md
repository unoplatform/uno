# Implementation Plan: WinAppSDK Source Generator Package

**Branch**: `001-winappsdk-sourcegen-package` | **Date**: 2026-03-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-winappsdk-sourcegen-package/spec.md`

## Summary

Create a new NuGet package `Uno.UI.SourceGenerators.WinAppSDK` containing only the code-behind source generator, specifically for WinAppSDK target builds. This replaces the current fragile workaround where `Uno.UI.SourceGenerators` is removed at build time and then re-added in a restricted "code-behind-only" mode. The new package is self-contained, automatically referenced for WinAppSDK targets via the Uno SDK, and completely isolated from the full set of Uno source generators.

## Technical Context

**Language/Version**: C# / .NET Standard 2.0 (analyzer target), .NET 10.0 (tests)
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp (v4.0.1), System.Xml.Linq (BCL)
**Storage**: N/A (build-time source generator)
**Testing**: MSTest + `CSharpIncrementalSourceGeneratorVerifier` (Roslyn testing framework)
**Target Platform**: WinAppSDK (Windows 10/11) — the generator runs during compilation
**Project Type**: Roslyn source generator (analyzer) packaged as NuGet
**Performance Goals**: N/A — build-time only, negligible overhead
**Constraints**: Must target `netstandard2.0` for Roslyn analyzer compatibility
**Scale/Scope**: ~340 lines of generator code, 4 source files + 1 diagnostics file + 1 props file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. WinUI API Fidelity | **PASS** | No new public API — packaging change only |
| II. Cross-Platform Parity | **PASS** | No behavioral change; Uno Platform targets unaffected |
| III. Test-First Quality Gates | **PASS** | Existing tests will be migrated to reference new assembly |
| IV. Performance and Resource Discipline | **PASS** | No runtime impact; build-time only |
| V. Generated Code Boundaries | **PASS** | No changes to generated code output format |
| VI. Backward Compatibility | **PASS** | No breaking changes — same generator behavior, different package |
| VII. WinUI Implementation Alignment | **N/A** | No WinUI behavior implementation involved |

No violations. All gates pass.

## Project Structure

### Documentation (this feature)

```text
specs/001-winappsdk-sourcegen-package/
├── plan.md              # This file
├── research.md          # Phase 0: Research decisions
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Build & verify guide
├── contracts/
│   └── msbuild-contract.md  # MSBuild props/targets contract
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/SourceGenerators/
├── Uno.UI.SourceGenerators/                    # EXISTING — remove code-behind files
│   ├── Uno.UI.SourceGenerators.csproj
│   ├── Content/
│   │   └── Uno.UI.SourceGenerators.props       # MODIFY — remove UnoCodeBehindGeneratorOnly
│   └── XamlGenerator/
│       ├── XamlCodeBehindGenerator.cs          # DELETE (moved to new project)
│       ├── XamlCodeBehindParser.cs             # DELETE (moved to new project)
│       ├── XamlCodeBehindEmitter.cs            # DELETE (moved to new project)
│       └── XamlClassInfo.cs                    # DELETE (moved to new project)
│
├── Uno.UI.SourceGenerators.WinAppSDK/          # NEW PROJECT
│   ├── Uno.UI.SourceGenerators.WinAppSDK.csproj
│   ├── Content/
│   │   └── Uno.UI.SourceGenerators.WinAppSDK.props
│   ├── XamlCodeBehindGenerator.cs
│   ├── XamlCodeBehindParser.cs
│   ├── XamlCodeBehindEmitter.cs
│   ├── XamlClassInfo.cs
│   └── XamlCodeBehindDiagnostics.cs
│
└── Uno.UI.SourceGenerators.Tests/              # MODIFY — add reference to new project
    └── XamlCodeBehindGeneratorTests/
        └── XamlCodeBehindGeneratorTests.cs     # MODIFY — update type reference

src/Uno.Sdk/targets/
├── Uno.UI.SourceGenerators.WinAppSdk.props     # MODIFY — simplify to reference new package
└── Uno.Common.WinAppSdk.targets                # NO CHANGE (imports WinAppSdk.props)

build/nuget/
├── Uno.UI.SourceGenerators.WinAppSDK.nuspec    # NEW — NuGet package spec
├── uno.winui.winappsdk.targets                 # MODIFY — simplify (remove re-add logic)
└── Uno.WinUI.nuspec                            # POSSIBLE CHANGE — verify no code-behind refs
```

**Structure Decision**: New project follows existing convention at `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/`, mirroring the structure of `Uno.UI.SourceGenerators` but containing only code-behind generator files. NuGet packaging follows existing `build/nuget/` convention.

## Complexity Tracking

No constitution violations — no entries needed.
