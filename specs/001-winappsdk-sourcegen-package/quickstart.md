# Quickstart: WinAppSDK Source Generator Package

**Date**: 2026-03-13
**Feature**: 001-winappsdk-sourcegen-package

## Overview

Create a new NuGet package `Uno.UI.SourceGenerators.WinAppSDK` containing only the code-behind source generator for WinAppSDK targets. This replaces the current workaround where `Uno.UI.SourceGenerators` is removed and re-added in a gated mode.

## Key Files to Create

1. **New project**: `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Uno.UI.SourceGenerators.WinAppSDK.csproj`
2. **Generator source files** (copied from `Uno.UI.SourceGenerators/XamlGenerator/`):
   - `XamlCodeBehindGenerator.cs`
   - `XamlCodeBehindParser.cs`
   - `XamlCodeBehindEmitter.cs`
   - `XamlClassInfo.cs`
   - `XamlCodeBehindDiagnostics.cs` (new — contains `UNOB0001` descriptor)
3. **Props file**: `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Content/Uno.UI.SourceGenerators.WinAppSDK.props`
4. **NuGet spec**: `build/nuget/Uno.UI.SourceGenerators.WinAppSDK.nuspec`

## Key Files to Modify

1. **Remove from Uno.UI.SourceGenerators**:
   - Delete `XamlCodeBehindGenerator.cs`, `XamlCodeBehindParser.cs`, `XamlCodeBehindEmitter.cs`, `XamlClassInfo.cs`
   - Remove `UnoCodeBehindGeneratorOnly` from `Uno.UI.SourceGenerators.props`
2. **Simplify WinAppSDK targets**:
   - `src/Uno.Sdk/targets/Uno.UI.SourceGenerators.WinAppSdk.props` — replace with new package reference
   - `build/nuget/uno.winui.winappsdk.targets` — simplify `_RemoveRoslynUnoSourceGenerationWinUI` (no re-add needed)
3. **Update NuGet packaging**:
   - `build/nuget/Uno.WinUI.nuspec` — remove code-behind-related `UnoCodeBehindGeneratorOnly` props reference
4. **Update tests**:
   - `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/` — add reference to new project, update generator type references

## Build & Verify

```bash
# Build the new package
cd src
dotnet build SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Uno.UI.SourceGenerators.WinAppSDK.csproj

# Run code-behind generator tests
dotnet test SourceGenerators/Uno.UI.SourceGenerators.Tests/Uno.UI.SourceGenerators.Tests.csproj --filter "FullyQualifiedName~XamlCodeBehind"

# Build Uno.UI to verify no regressions
dotnet build Uno.UI-UnitTests-only.slnf --no-restore
dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build
```
