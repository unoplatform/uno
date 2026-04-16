# Quickstart: Global/Implicit XAML Namespaces

## For Developers Using the Feature

### Basic Usage (Zero Configuration)

With Uno.Sdk, implicit XAML namespaces are enabled by default. Write XAML without `xmlns` declarations:

```xml
<!-- Before -->
<Page
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Class="MyApp.MainPage">
    <StackPanel>
        <TextBlock Text="Hello" />
        <Button Content="Click me" />
    </StackPanel>
</Page>

<!-- After -->
<Page x:Class="MyApp.MainPage">
    <StackPanel>
        <TextBlock Text="Hello" />
        <Button Content="Click me" />
    </StackPanel>
</Page>
```

### Register Custom Namespaces

Create a `GlobalXmlns.cs` in your project root:

```csharp
using Microsoft.UI.Xaml;

[assembly: XmlnsDefinition(
    "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
    "MyApp.ViewModels")]
[assembly: XmlnsDefinition(
    "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
    "MyApp.Converters")]
```

Now use those types directly in XAML:
```xml
<Page x:Class="MyApp.MainPage" x:DataType="MainViewModel">
    <TextBlock Text="{x:Bind Name}" />
</Page>
```

### Opt Out

```xml
<PropertyGroup>
    <UnoEnableImplicitXamlNamespaces>false</UnoEnableImplicitXamlNamespaces>
</PropertyGroup>
```

## For Contributors Implementing the Feature

### Build & Test

```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props
# Edit to set: <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride>

dotnet build Uno.UI-Skia-only.slnf
dotnet test Uno.UI/Uno.UI.Tests.csproj
```

### Key Files

1. **Source generator parsing**: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileParser.cs`
2. **Type resolution**: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.Reflection.cs`
3. **Constants**: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlConstants.cs`
4. **MSBuild config**: `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props`
5. **Uno.Sdk defaults**: `src/Uno.Sdk/targets/`
6. **Runtime tests**: `src/Uno.UI.RuntimeTests/`

### Runtime Test Execution

```bash
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```
