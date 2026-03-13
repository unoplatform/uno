# MSBuild Contract: Uno.UI.SourceGenerators.WinAppSDK

**Date**: 2026-03-13
**Feature**: 001-winappsdk-sourcegen-package

## Package Structure

```
Uno.UI.SourceGenerators.WinAppSDK.nupkg
├── analyzers/
│   └── dotnet/
│       └── cs/
│           ├── Uno.UI.SourceGenerators.WinAppSDK.dll
│           └── Uno.UI.SourceGenerators.WinAppSDK.pdb
└── buildTransitive/
    └── Uno.UI.SourceGenerators.WinAppSDK.props
```

## Props File Contract (Uno.UI.SourceGenerators.WinAppSDK.props)

The `.props` file is automatically imported for any project referencing this package. It configures:

### Compiler-Visible Properties

```xml
<ItemGroup>
  <CompilerVisibleProperty Include="UnoGenerateCodeBehind" />
  <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="SourceItemGroup" />
  <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="UnoGenerateCodeBehind" />
</ItemGroup>
```

### AdditionalFiles Injection

A target that runs before `GenerateMSBuildEditorConfigFileShouldRun` injects `Page` and `ApplicationDefinition` items into `AdditionalFiles` with `SourceItemGroup` metadata.

```xml
<Target Name="_InjectAdditionalFilesForWinAppSDK"
        BeforeTargets="GenerateMSBuildEditorConfigFileShouldRun;GenerateMSBuildEditorConfigFileCore">
  <ItemGroup>
    <AdditionalFiles Include="@(Page)" SourceItemGroup="Page" />
    <AdditionalFiles Include="@(ApplicationDefinition)" SourceItemGroup="ApplicationDefinition" />
  </ItemGroup>
</Target>
```

## Generator Behavior Contract

### Input
- `AdditionalFiles` with `SourceItemGroup` metadata set to `Page` or `ApplicationDefinition`
- `build_property.UnoGenerateCodeBehind` (global, default: `true`)
- `build_metadata.AdditionalFiles.UnoGenerateCodeBehind` (per-file override)

### Output
- For each XAML file with `x:Class` where no class exists in compilation:
  - Source file: `{FullClassName}.codebehind.g.cs`
  - Content: Partial class with parameterless constructor calling `this.InitializeComponent()`

### Diagnostics
- `UNOB0001` (Warning): Malformed `x:Class` value (missing namespace)

## Removed Contracts (from Uno.UI.SourceGenerators)

The following MSBuild elements are removed or simplified:

| Element                                  | Action     | Reason                                         |
|------------------------------------------|------------|-------------------------------------------------|
| `UnoCodeBehindGeneratorOnly` property     | Removed    | No longer needed — dedicated package always runs |
| `_AddCodeBehindGeneratorForWinUI` target  | Removed    | No remove-and-re-add needed                     |
| `_InjectAdditionalFilesForWinUI` target   | Removed    | Replaced by props in new package                 |
| `_RemoveRoslynUnoSourceGenerationWinUI` target (partial) | Simplified | Still removes Uno.UI.SourceGenerators but no re-add |
