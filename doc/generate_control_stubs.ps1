#!/usr/bin/env pwsh
# Generate stub pages for implemented controls

$controls = @(
    @{Name="Animated Icon"; File="microsoft-ui-xaml-controls-animatedicon"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="App Bar"; File="microsoft-ui-xaml-controls-appbar"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="App Bar Button"; File="microsoft-ui-xaml-controls-appbarbutton"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Auto Suggest Box"; File="microsoft-ui-xaml-controls-autosuggestbox"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Border"; File="microsoft-ui-xaml-controls-border"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Canvas"; File="microsoft-ui-xaml-controls-canvas"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Check Box"; File="microsoft-ui-xaml-controls-checkbox"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Combo Box"; File="microsoft-ui-xaml-controls-combobox"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Content Control"; File="microsoft-ui-xaml-controls-contentcontrol"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Date Picker"; File="microsoft-ui-xaml-controls-datepicker"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Frame"; File="microsoft-ui-xaml-controls-frame"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Grid"; File="microsoft-ui-xaml-controls-grid"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Grid View"; File="microsoft-ui-xaml-controls-gridview"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Image"; File="microsoft-ui-xaml-controls-image"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="List View"; File="microsoft-ui-xaml-controls-listview"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Navigation View"; File="microsoft-ui-xaml-controls-navigationview"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Password Box"; File="microsoft-ui-xaml-controls-passwordbox"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Progress Bar"; File="microsoft-ui-xaml-controls-progressbar"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Progress Ring"; File="microsoft-ui-xaml-controls-progressring"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Radio Button"; File="microsoft-ui-xaml-controls-radiobutton"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Scroll Viewer"; File="microsoft-ui-xaml-controls-scrollviewer"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Stack Panel"; File="microsoft-ui-xaml-controls-stackpanel"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Text Block"; File="microsoft-ui-xaml-controls-textblock"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Text Box"; File="microsoft-ui-xaml-controls-textbox"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="Toggle Switch"; File="microsoft-ui-xaml-controls-toggleswitch"; Namespace="Microsoft.UI.Xaml.Controls"},
    @{Name="WebView2"; File="microsoft-ui-xaml-controls-webview2"; Namespace="Microsoft.UI.Xaml.Controls"}
)

$outputDir = "./articles/implemented"

foreach ($control in $controls) {
    $fileName = "$($control.File).md"
    $filePath = Join-Path $outputDir $fileName
    
    # Skip if file already exists
    if (Test-Path $filePath) {
        Write-Host "Skipping $fileName (already exists)"
        continue
    }
    
    $content = @"
# $($control.Name)

**Namespace:** $($control.Namespace)

**Platforms:** WASM, Skia, Mobile

The ``$($control.Name)`` control is implemented in Uno Platform across supported platforms.

## Implementation Status

✅ **Implemented** across WebAssembly, iOS, Android, macOS, Skia, and tvOS

## Overview

For complete API documentation and usage examples, see the [Microsoft WinUI $($control.Name) Documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/$($control.Namespace.ToLower()).$($control.Name.Replace(' ', '').ToLower())).

## Platform Support

This control is available on:
- WebAssembly
- iOS
- Android  
- macOS (Catalyst)
- Skia (Desktop platforms)
- tvOS

## Key Features

The control includes support for:
- Standard WinUI properties and events
- Data binding
- Styling and theming
- Accessibility features

## Styling

The control supports:
- Fluent Design System (default)
- Material Design (via Uno.Themes)
- Cupertino/iOS styling (via Uno.Themes)
- Light and dark themes

## See Also

- [Microsoft WinUI $($control.Name) Documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/$($control.Namespace.ToLower()).$($control.Name.Replace(' ', '').ToLower()))
- [Uno Platform Samples](https://aka.platform.uno/wasm-samples-app)
- [Implemented Controls Overview](../implemented-views.md)
"@

    Set-Content -Path $filePath -Value $content -Encoding UTF8
    Write-Host "Created $fileName"
}

Write-Host "`nGenerated $($controls.Count) control documentation stubs"
