---
uid: Uno.Features.ImplicitXamlNamespaces
---

# Implicit XAML Namespaces

Uno Platform supports implicit XAML namespaces (also known as XAML namespace mapping), allowing you to write XAML files without repetitive `xmlns` boilerplate declarations. This feature is inspired by .NET MAUI's global usings for XAML and is enabled by default in Uno.Sdk projects.

Conceptually, this is the same mechanism WPF has long exposed through the `XmlnsDefinition` and `XmlnsPrefix` assembly attributes in `System.Windows.Markup`, described in [XAML Namespaces and Namespace Mapping for WPF XAML](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/xaml-namespaces-and-namespace-mapping-for-wpf-xaml). Uno Platform uses the same attributes, the same `System.Windows.Markup` namespace, and the same well-known URIs, and extends the model so that the default presentation namespace can be opted into from application and library assemblies across all Uno targets.

## Overview

Traditionally, every XAML file requires at minimum:

```xml
<Page
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Class="MyApp.MainPage">
    <StackPanel>
        <Button Content="Hello" />
        <TextBlock Text="World" />
    </StackPanel>
</Page>
```

With implicit XAML namespaces enabled, the same page becomes:

```xml
<Page x:Class="MyApp.MainPage">
    <StackPanel>
        <Button Content="Hello" />
        <TextBlock Text="World" />
    </StackPanel>
</Page>
```

The default WinUI presentation namespace (`xmlns`) and the XAML namespace (`xmlns:x`) are implicitly available without explicit declaration.

## How It Works

When `UnoEnableImplicitXamlNamespaces` is set to `true` (the default), the Uno Platform source generator automatically injects the missing `xmlns` declarations into the root element of each XAML file before parsing. This happens transparently at compile time and does not modify your source files.

The following namespaces are implicitly available:

| Namespace | Prefix | URI |
|-----------|--------|-----|
| WinUI Presentation | *(default)* | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` |
| XAML Language | `x:` | `http://schemas.microsoft.com/winfx/2006/xaml` |

## Relationship to WPF XAML Namespace Mapping

If you are familiar with WPF's [XAML namespaces and namespace mapping](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/xaml-namespaces-and-namespace-mapping-for-wpf-xaml), the model used by Uno Platform will feel identical — the attribute names, the assembly namespace (`System.Windows.Markup`), and the well-known URIs are the same:

| Concept | WPF | Uno Platform |
|---------|-----|--------------|
| Map an XML namespace URI to one or more CLR namespaces | `[assembly: XmlnsDefinition("…", "CLR.Namespace")]` | Same attribute, same usage |
| Recommend a prefix for an XML namespace URI | `[assembly: XmlnsPrefix("…", "prefix")]` | Same attribute, same usage |
| Default presentation namespace URI | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` (maps to WPF controls) | Same URI (maps to WinUI controls) |
| XAML language namespace URI | `http://schemas.microsoft.com/winfx/2006/xaml` | Same URI |

Developers porting from WPF, or using WPF as a mental model, can apply the same knowledge directly: assembly-level `XmlnsDefinition` and `XmlnsPrefix` attributes declare mappings, and Uno Platform honors those mappings for every Uno target (WebAssembly, Skia Desktop, Android, iOS).

Uno Platform additionally defines a *global* namespace URI — `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global` — which extends the WPF model. CLR namespaces mapped to this URI become part of the implicit default namespace, so their types are available unprefixed in XAML. WPF has no direct equivalent: in WPF, user assemblies cannot extend the default presentation namespace this way.

## Registering Custom Namespaces

You can register your own CLR namespaces to be available globally (unprefixed) in XAML by using the `XmlnsDefinition` attribute targeting the global namespace URI. This is the same attribute WPF uses for [XAML namespace mapping](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/xaml-namespaces-and-namespace-mapping-for-wpf-xaml) — the difference is only the target URI:

```csharp
[assembly: System.Windows.Markup.XmlnsDefinition(
    "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
    "MyApp.Controls")]
```

Types from registered namespaces resolve unprefixed in XAML:

```xml
<Page x:Class="MyApp.MainPage">
    <StackPanel>
        <!-- MyCustomControl is from MyApp.Controls, resolved via global namespace -->
        <MyCustomControl Label="Hello" />
        <Button Content="Standard WinUI Button" />
    </StackPanel>
</Page>
```

> [!NOTE]
> Standard WinUI types always take precedence over custom types with the same name. If your global namespace contains a type named `Button`, the WinUI `Button` will be resolved instead.

### Registering Prefixed Namespaces

You can make a CLR namespace implicitly available with a specific prefix using `XmlnsPrefix`:

```csharp
using System.Windows.Markup;

[assembly: XmlnsPrefix("MyApp.Controls.Advanced", "adv")]
```

The `adv:` prefix is then available in XAML without declaring `xmlns:adv`:

```xml
<Page x:Class="MyApp.MainPage">
    <StackPanel>
        <adv:AdvancedControl Value="42" />
    </StackPanel>
</Page>
```

Alternatively, if you already have a custom XML namespace URI defined via `XmlnsDefinition`, you can associate a prefix with that URI:

```csharp
using System.Windows.Markup;

[assembly: XmlnsDefinition("http://myapp.com/controls", "MyApp.Controls.Advanced")]
[assembly: XmlnsPrefix("http://myapp.com/controls", "adv")]
```

## Cross-Assembly Support

The `XmlnsDefinition` attribute works across assembly boundaries. If a referenced library declares:

```csharp
[assembly: XmlnsDefinition(
    "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
    "MyLibrary.Controls")]
```

Types from `MyLibrary.Controls` will automatically be available unprefixed in any consuming project that has implicit XAML namespaces enabled.

## Backward Compatibility

Implicit XAML namespaces are fully backward compatible:

- XAML files with explicit `xmlns` declarations continue to work identically
- Explicit per-file `xmlns` declarations take precedence over implicit ones
- The `mc:Ignorable`, `d:DesignHeight`, and other design-time attributes work alongside implicit namespaces (just declare `xmlns:mc` and `xmlns:d` as usual)

## Disambiguation

If two globally registered namespaces contain types with the same name, use an explicit `xmlns` prefix to resolve the ambiguity:

```xml
<Page x:Class="MyApp.MainPage"
      xmlns:libA="using:LibraryA.Controls"
      xmlns:libB="using:LibraryB.Controls">
    <StackPanel>
        <libA:SharedTypeName />
        <libB:SharedTypeName />
    </StackPanel>
</Page>
```

## Configuration

### Disabling Implicit Namespaces

To opt out of implicit XAML namespaces, set the property in your project file or `Directory.Build.props`:

```xml
<PropertyGroup>
    <UnoEnableImplicitXamlNamespaces>false</UnoEnableImplicitXamlNamespaces>
</PropertyGroup>
```

When disabled, all XAML files must include explicit `xmlns` declarations as before.

### Custom Global Namespace URI

The default global namespace URI is `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global`. You can customize this if needed:

```xml
<PropertyGroup>
    <UnoGlobalXamlNamespaceUri>http://myapp.com/global</UnoGlobalXamlNamespaceUri>
</PropertyGroup>
```

## MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `UnoEnableImplicitXamlNamespaces` | `true` | Enables/disables implicit XAML namespace injection |
| `UnoGlobalXamlNamespaceUri` | `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global` | URI for registering custom CLR namespaces as globally available |

## Limitations

- Implicit namespaces are a compile-time feature. IDE XAML designers may not fully support the reduced syntax until they are updated.
- Hot Reload correctly handles XAML without explicit xmlns declarations.
- The feature requires Uno.Sdk. Projects not using Uno.Sdk need to manually set `UnoEnableImplicitXamlNamespaces` to `true` in their project file.
