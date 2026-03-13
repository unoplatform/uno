---
uid: Uno.Features.AutoCodeBehind
---

# Auto-Generated Code-Behind

Uno Platform can automatically generate minimal code-behind files for XAML pages that don't have a developer-authored `.xaml.cs` file. This eliminates boilerplate when your page only needs a default constructor that calls `InitializeComponent()`.

## Overview

When you create a XAML file with an `x:Class` attribute but no corresponding `.xaml.cs` file, the build system automatically generates a partial class with a constructor that calls `InitializeComponent()`. This lets you create XAML-only pages without writing any C# boilerplate.

### Before (traditional approach)

You need two files:

**MainPage.xaml**

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <TextBlock Text="Hello, World!" />
</Page>
```

**MainPage.xaml.cs**

```csharp
namespace MyApp
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
    }
}
```

### After (with auto code-behind)

You only need the XAML file:

**MainPage.xaml**

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <TextBlock Text="Hello, World!" />
</Page>
```

The code-behind is generated automatically at build time. The generated class inherits from the correct base type based on the XAML root element.

## Supported root elements

The following XAML root elements are supported with their corresponding base types:

| Root Element | Generated Base Type |
|---|---|
| `Page` | `Microsoft.UI.Xaml.Controls.Page` |
| `UserControl` | `Microsoft.UI.Xaml.Controls.UserControl` |
| `Window` | `Microsoft.UI.Xaml.Window` |
| `ContentDialog` | `Microsoft.UI.Xaml.Controls.ContentDialog` |
| `Application` | `Microsoft.UI.Xaml.Application` |
| `ResourceDictionary` | `Microsoft.UI.Xaml.ResourceDictionary` |

Custom root elements using `using:` or `clr-namespace:` xmlns prefixes are also supported.

## How it works

1. During build, the source generator scans all XAML files with an `x:Class` attribute.
2. If the class specified in `x:Class` does not already exist in the compilation (no `.xaml.cs` or other source file defining it), a minimal partial class is generated.
3. If a developer-authored code-behind file already exists, no code is generated — the existing file takes full precedence.
4. The generated code includes a default constructor that calls `this.InitializeComponent()`.

### When to use auto code-behind

Auto code-behind works best for XAML pages that:

- Use only `x:Bind` or `{Binding}` for data binding
- Use `x:Bind` for event handlers pointing to a view model
- Don't require custom logic in the constructor
- Don't need code-behind event handlers

### When to keep a manual code-behind

You still need a manual `.xaml.cs` file when you need to:

- Add constructor parameters or custom initialization logic
- Handle events in code-behind (not via `x:Bind`)
- Override lifecycle methods (`OnNavigatedTo`, etc.)
- Implement interfaces on the page class

## Configuration

### Project-level control

You can disable auto code-behind generation for the entire project by setting the `UnoGenerateCodeBehind` MSBuild property in your `.csproj`:

```xml
<PropertyGroup>
    <UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>
</PropertyGroup>
```

The default value is `true` (enabled).

### Per-file control

You can override the project-level setting for individual XAML files using the `UnoGenerateCodeBehind` item metadata:

```xml
<!-- Disable auto code-behind for a specific file -->
<Page Update="MainPage.xaml" UnoGenerateCodeBehind="false" />

<!-- Enable auto code-behind for a specific file even when globally disabled -->
<Page Update="SimplePage.xaml" UnoGenerateCodeBehind="true" />
```

Per-file metadata always takes precedence over the project-level property.

## Diagnostics

| Code | Severity | Description |
|---|---|---|
| UNOB0001 | Warning | The `x:Class` attribute value is malformed. The value must include a namespace (e.g., `MyApp.MainPage`, not just `MainPage`). |

## Platform support

This feature works on all platforms supported by Uno Platform:

| Feature | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) |
|---|---|---|---|---|---|---|
| Auto code-behind generation | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Per-file configuration | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Project-level configuration | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |

On Uno Platform targets, code-behind generation is handled by the integrated XAML source generation pipeline. On WinUI (Windows) targets, a standalone incremental source generator provides the same functionality.
