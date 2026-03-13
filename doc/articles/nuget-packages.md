---
uid: Uno.Development.NuGetPackages
---

# Uno Platform NuGet Packages

This article provides a comprehensive list of all first-party NuGet packages published by Uno Platform. These packages enable you to build cross-platform applications targeting Windows, macOS, Linux, iOS, Android, and WebAssembly.

## Core Packages

### Uno.SDK

[![NuGet](https://img.shields.io/nuget/v/Uno.Sdk.svg)](https://www.nuget.org/packages/Uno.Sdk/)

The Uno SDK is a modern MSBuild SDK that simplifies project structure and package management. It's the recommended way to create new Uno Platform projects.

- **Description**: Build system SDK that provides streamlined project configuration
- **Documentation**: [Using the Uno.SDK](xref:Uno.Features.Uno.Sdk)
- **When to use**: All new Uno Platform projects (5.1+)

### Uno.WinUI / Uno.UI

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.svg)](https://www.nuget.org/packages/Uno.WinUI/)  
[![NuGet](https://img.shields.io/nuget/v/Uno.UI.svg)](https://www.nuget.org/packages/Uno.UI/)

The main UI framework packages that implement the WinUI 3 and UWP APIs across all platforms.

- **Uno.WinUI**: WinUI 3 API implementation for cross-platform development
- **Uno.UI**: Legacy UWP API implementation (for existing projects)
- **Description**: Build Mobile, Desktop and WebAssembly apps with C# and XAML
- **Documentation**: [Uno and WinUI 3](uwp-vs-winui3.md)
- **When to use**: Core framework for all Uno Platform applications

### Uno.WinRT

[![NuGet](https://img.shields.io/nuget/v/Uno.WinRT.svg)](https://www.nuget.org/packages/Uno.WinRT/)

Provides Windows Runtime (WinRT) API implementations across platforms.

- **Description**: Cross-platform implementation of Windows Runtime APIs
- **Documentation**: [Uno.WinRT](xref:uno.features.uno.winrt)
- **When to use**: Automatically included as a dependency of Uno.WinUI/Uno.UI

### Uno.Foundation

[![NuGet](https://img.shields.io/nuget/v/Uno.Foundation.svg)](https://www.nuget.org/packages/Uno.Foundation/)

Foundation types and core functionality shared across Uno Platform.

- **Description**: Core types and utilities for Uno Platform
- **When to use**: Automatically included as a dependency of Uno.WinUI/Uno.UI

## Platform-Specific Packages

### Uno.WinUI.WebAssembly

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.WebAssembly.svg)](https://www.nuget.org/packages/Uno.WinUI.WebAssembly/)

WebAssembly-specific implementation and bootstrapper.

- **Description**: Enables running Uno Platform apps in web browsers via WebAssembly
- **Documentation**: [Publishing for WebAssembly](xref:uno.publishing.webassembly)

### Uno.WinUI.Skia.Gtk

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Skia.Gtk.svg)](https://www.nuget.org/packages/Uno.WinUI.Skia.Gtk/)

Skia-based rendering for Linux desktop using GTK.

- **Description**: Linux desktop support using Skia rendering and GTK
- **Documentation**: [Skia Desktop](xref:Uno.Skia.Desktop)

### Uno.WinUI.Skia.Wpf

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Skia.Wpf.svg)](https://www.nuget.org/packages/Uno.WinUI.Skia.Wpf/)

Skia-based rendering for Windows desktop using WPF.

- **Description**: Windows desktop support using Skia rendering and WPF
- **Documentation**: [Skia Desktop](xref:Uno.Skia.Desktop)

### Uno.WinUI.Skia.MacOS

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Skia.MacOS.svg)](https://www.nuget.org/packages/Uno.WinUI.Skia.MacOS/)

Skia-based rendering for macOS desktop.

- **Description**: macOS desktop support using Skia rendering
- **Documentation**: [Using Skia Desktop (macOS)](xref:Uno.Skia.macOS)

### Uno.WinUI.Skia.Linux.FrameBuffer

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Skia.Linux.FrameBuffer.svg)](https://www.nuget.org/packages/Uno.WinUI.Skia.Linux.FrameBuffer/)

Framebuffer rendering for Linux embedded systems.

- **Description**: Run Uno apps directly on Linux framebuffer without X11/Wayland
- **Documentation**: [Linux Framebuffer](features/using-linux-framebuffer.md)

### Uno.WinUI.Skia.X11

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Skia.X11.svg)](https://www.nuget.org/packages/Uno.WinUI.Skia.X11/)

X11-based rendering for Linux desktop.

- **Description**: Linux desktop support using direct X11 rendering

## UI Enhancement Packages

### Uno.WinUI.Graphics2DSK

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Graphics2DSK.svg)](https://www.nuget.org/packages/Uno.WinUI.Graphics2DSK/)

SkiaSharp integration for 2D graphics rendering.

- **Description**: Provides SKCanvasElement for SkiaSharp-based custom rendering
- **Documentation**: [SKCanvasElement](xref:Uno.Controls.SKCanvasElement)
- **When to use**: When you need custom 2D graphics using SkiaSharp

### Uno.WinUI.Lottie

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Lottie.svg)](https://www.nuget.org/packages/Uno.WinUI.Lottie/)

Support for Lottie animations.

- **Description**: Display and control Lottie animation files
- **Documentation**: [Lottie animations](features/Lottie.md)
- **When to use**: To display vector animations from Adobe After Effects

### Uno.WinUI.Svg

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Svg.svg)](https://www.nuget.org/packages/Uno.WinUI.Svg/)

SVG image support.

- **Description**: Enhanced SVG rendering capabilities
- **Documentation**: [Using SVG images](features/svg.md)
- **When to use**: For advanced SVG image support

### Uno.WinUI.MSAL

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.MSAL.svg)](https://www.nuget.org/packages/Uno.WinUI.MSAL/)

Microsoft Authentication Library (MSAL) integration.

- **Description**: Azure AD/Microsoft authentication support
- **Documentation**: [MSAL for Azure Authentication](xref:Uno.Interop.MSAL)
- **When to use**: To implement Microsoft identity platform authentication

### Uno.WinUI.Maps

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.Maps.svg)](https://www.nuget.org/packages/Uno.WinUI.Maps/)

Map control implementation.

- **Description**: Interactive map support using native map controls
- **Documentation**: [MapControl](controls/map-control-support.md)
- **When to use**: To display interactive maps in your application

### Uno.WinUI.MediaPlayerElement

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.MediaPlayerElement.svg)](https://www.nuget.org/packages/Uno.WinUI.MediaPlayerElement/)

Media playback control.

- **Description**: Video and audio playback using native media players
- **Documentation**: [MediaPlayerElement](controls/MediaPlayerElement.md)
- **When to use**: To play video or audio content

## Developer Tools

### Uno.WinUI.DevServer

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.DevServer.svg)](https://www.nuget.org/packages/Uno.WinUI.DevServer/)

Development server for enhanced debugging.

- **Description**: Hot reload and debugging support
- **Documentation**: [Hot Reload](xref:Uno.Features.HotReload)
- **When to use**: Automatically included in debug builds

### Uno.Resizetizer

[![NuGet](https://img.shields.io/nuget/v/Uno.Resizetizer.svg)](https://www.nuget.org/packages/Uno.Resizetizer/)

Image and asset resizing tool.

- **Description**: Automatically generates app icons and splash screens from SVG files
- **Documentation**: [Splash Screen](xref:Uno.Development.SplashScreen)
- **When to use**: Included by default in Uno Platform 4.8+ projects

### Uno.UITest

[![NuGet](https://img.shields.io/nuget/v/Uno.UITest.svg)](https://www.nuget.org/packages/Uno.UITest/)

UI testing framework.

- **Description**: Cross-platform UI testing based on Xamarin.UITest
- **Documentation**: [Creating UI Tests](uno-development/creating-ui-tests.md)
- **When to use**: To create automated UI tests

## WPF Integration

### Uno.WinUI.XamlHost

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.XamlHost.svg)](https://www.nuget.org/packages/Uno.WinUI.XamlHost/)

Host Uno Platform controls in WPF applications.

- **Description**: Embed Uno Platform UI in WPF applications
- **Documentation**: [Uno Platform in WPF](guides/uno-islands.md)
- **When to use**: To modernize existing WPF applications

### Uno.WinUI.XamlHost.Skia.Wpf

[![NuGet](https://img.shields.io/nuget/v/Uno.WinUI.XamlHost.Skia.Wpf.svg)](https://www.nuget.org/packages/Uno.WinUI.XamlHost.Skia.Wpf/)

Skia-based XAML hosting for WPF.

- **Description**: Skia renderer version of XamlHost for WPF
- **Documentation**: [Uno Platform in WPF](guides/uno-islands.md)

## Supporting Packages

### Uno.Fonts.Fluent

[![NuGet](https://img.shields.io/nuget/v/Uno.Fonts.Fluent.svg)](https://www.nuget.org/packages/Uno.Fonts.Fluent/)

Fluent icon font.

- **Description**: Microsoft Fluent icon font for UI icons
- **Documentation**: [Fluent icon font](uno-fluent-assets.md)
- **When to use**: Automatically included with Uno.WinUI/Uno.UI

### Uno.Foundation.Logging

[![NuGet](https://img.shields.io/nuget/v/Uno.Foundation.Logging.svg)](https://www.nuget.org/packages/Uno.Foundation.Logging/)

Logging infrastructure.

- **Description**: Core logging interfaces and utilities
- **Documentation**: [Logging](logging.md)
- **When to use**: Automatically included as a dependency

### Uno.Diagnostics.Eventing

[![NuGet](https://img.shields.io/nuget/v/Uno.Diagnostics.Eventing.svg)](https://www.nuget.org/packages/Uno.Diagnostics.Eventing/)

Event tracing support.

- **Description**: Performance monitoring and diagnostics
- **When to use**: Automatically included as a dependency

### Uno.Core

[![NuGet](https://img.shields.io/nuget/v/Uno.Core.svg)](https://www.nuget.org/packages/Uno.Core/)

Core utilities and extensions.

- **Description**: Common utilities used across Uno Platform packages
- **When to use**: For advanced scenarios and internal utilities

## Additional Ecosystems

For packages related to Uno Platform extensions, themes, and toolkits, see:

- **[Uno.Extensions](https://aka.platform.uno/uno-extensions-docs)** - MVUX, Navigation, Dependency Injection, Configuration, HTTP, Authentication, and more
- **[Uno.Toolkit](https://aka.platform.uno/uno-toolkit-docs)** - Additional UI controls and behaviors
- **[Uno.Themes](https://aka.platform.uno/uno-themes-docs)** - Material and Cupertino design system implementations
- **[Uno.Check](https://www.nuget.org/packages/Uno.Check/)** - Environment setup and validation tool

## Related Resources

- [How to upgrade Uno Platform NuGet Packages](xref:Uno.Development.UpgradeUnoNuget)
- [Supported 3rd-party libraries](xref:Uno.Development.SupportedLibraries)
- [.NET Version Support](net-version-support.md)
- [Using the Uno.SDK](xref:Uno.Features.Uno.Sdk)

## Package Versioning

All core Uno Platform packages follow semantic versioning and are released in sync. When upgrading, ensure you update all Uno packages to the same version to avoid compatibility issues.

For projects using Uno.Sdk, the SDK version controls all underlying package versions automatically.

---

> [!TIP]
> If you find a package is missing from this list or have suggestions for improvements, please [create an issue](https://github.com/unoplatform/uno/issues/new/choose) or [submit a pull request](https://github.com/unoplatform/uno/blob/master/doc/articles/nuget-packages.md).
