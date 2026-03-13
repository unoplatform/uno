# Quickstart: Auto-Generate Code-Behind for XAML Files

**Feature**: 001-auto-codebehind | **Date**: 2026-03-05

## What This Feature Does

Automatically generates a code-behind partial class for `.xaml` files that have an `x:Class` attribute but no developer-authored code-behind class. The generated class contains a constructor that calls `InitializeComponent()`.

## Developer Experience

### Before (Status Quo)

To create a new page, developers must create two files:

**MainPage.xaml**:
```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <TextBlock Text="Hello World" />
</Page>
```

**MainPage.xaml.cs** (boilerplate):
```csharp
namespace MyApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
}
```

### After (With Auto Code-Behind)

Developers only create the XAML file. The code-behind is auto-generated at build time:

**MainPage.xaml** (only file needed):
```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <TextBlock Text="Hello World" />
</Page>
```

Build output automatically includes a generated `MainPage.codebehind.g.cs` with the constructor.

## Configuration

### Default Behavior

Auto code-behind generation is **enabled by default**. No configuration needed.

### Disable for Entire Project

```xml
<PropertyGroup>
  <UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>
</PropertyGroup>
```

### Disable for a Specific File

```xml
<ItemGroup>
  <Page Update="Views/SpecialPage.xaml">
    <UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>
  </Page>
</ItemGroup>
```

### Enable for a Specific File (when globally disabled)

```xml
<PropertyGroup>
  <UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>
</PropertyGroup>

<ItemGroup>
  <Page Update="Views/SimplePage.xaml">
    <UnoGenerateCodeBehind>true</UnoGenerateCodeBehind>
  </Page>
</ItemGroup>
```

## When Code-Behind is NOT Generated

- The XAML file has no `x:Class` attribute (e.g., standalone `ResourceDictionary`)
- A class matching the `x:Class` value already exists in the compilation
- The feature is disabled via MSBuild property or per-file metadata
- The `x:Class` value is malformed (a build diagnostic is emitted)

## Implementation Files

| File | Purpose |
|------|---------|
| `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs` | Modified: integrated code-behind generation for Uno targets |
| `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs` | Modified: suppress `#warning` and handle x:Bind when code-behind is auto-generated |
| `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/XamlCodeBehindGenerator.cs` | Standalone `IIncrementalGenerator` for WinUI targets only |
| `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props` | Modified: add CompilerVisibleProperty/Metadata for code-behind settings |
| `src/Uno.Sdk/targets/Uno.UI.SourceGenerators.WinAppSdk.props` | MSBuild plumbing for WinUI code-behind generation |

## Build Verification

```bash
# Create a XAML file without code-behind
# Build the project
dotnet build

# Verify the generated file exists in obj/ output
# The generated source will be under:
#   obj/{Config}/{TFM}/generated/Uno.UI.SourceGenerators/.../{ClassName}.g.cs
```
