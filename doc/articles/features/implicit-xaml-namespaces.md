---
uid: Uno.Features.ImplicitXamlNamespaces
---

# Implicit XAML Namespaces

Uno Platform supports implicit XAML namespaces, allowing you to write XAML files without repetitive `xmlns` boilerplate declarations. This feature is inspired by .NET MAUI's global usings for XAML and is enabled by default in Uno.Sdk projects.

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

## Registering Custom Namespaces

You can register your own CLR namespaces to be available globally (unprefixed) in XAML by using the `XmlnsDefinition` attribute targeting the global namespace URI:

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

You can also make a namespace implicitly available with a specific prefix using `XmlnsPrefix`:

```csharp
[assembly: System.Windows.Markup.XmlnsDefinition(
    "http://myapp.com/controls",
    "MyApp.Controls.Advanced")]
[assembly: System.Windows.Markup.XmlnsPrefix(
    "http://myapp.com/controls",
    "adv")]
```

The `adv:` prefix is then available in XAML without declaring `xmlns:adv`:

```xml
<Page x:Class="MyApp.MainPage">
    <StackPanel>
        <adv:AdvancedControl Value="42" />
    </StackPanel>
</Page>
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
