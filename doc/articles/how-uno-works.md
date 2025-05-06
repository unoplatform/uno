---
uid: Uno.Development.HowItWorks
---

# How Uno Platform Works

How does Uno Platform make the same application code run on all platforms?

## Uno Platform at Runtime

The [`Uno.WinUI` NuGet package](https://www.nuget.org/packages/Uno.WinUI/) implements the entire WinUI API surface across platforms. This includes:

- Namespaces like `Microsoft.UI.Xaml`, `Windows.Foundation`, `Windows.Storage`, and more.
- Classes, enums, and all relevant members.

> [!NOTE]
> As the API surface is very large, some parts are included but not implemented. These features are marked with the [`Uno.NotImplementedAttribute`] attribute, and a code analyzer is included with the Uno.WinUI package will generate a warning for any such features that are referenced. You can see a complete list of supported APIs [here](implemented-views.md).

## Rendering the UI

As a developer, you write your UI using the standard WinUI-based XAML. Under the hood, Uno Platform maps this visual tree to the appropriate rendering mechanism for each platform.

For every platform, the public API - panels, control templates, measurement and arranging, etc - is the same. However, it's sometimes useful to know what's going on at render-time, for example when [embedding native views inside XAML views](xref:Uno.Development.NativeViews) on a platform-specific basis.

Uno Platform provides two rendering modes, Skia and Native. The first one uses Skia to draw all the App pixels on the screen, while the second one uses the native platform APIs to render on the screen.

Skia is the default renderer in the **Blank** and **Recommended** Uno Platform project template presets.

### Skia Rendering

[Skia](https://skia.org) is a cross-platform, hardware-accelerated rendering engine used by Uno Platform to draw the entire visual tree. Unlike native rendering, Skia doesn’t rely on platform UI components.

Skia is the default rendering engine for Uno Platform as of Uno Platform 6.0 or later across **Desktop (Windows, macOS, Linux)**, **WebAssembly (WASM)**, **Android**, and **iOS**.

> [!IMPORTANT]
> **WinAppSDK** continues to use the official native rendering provided by Microsoft, as Uno Platform rendering is not present for this target.

It provides a **consistent, pixel-perfect rendering pipeline** by drawing the entire UI using the Skia graphics library (via SkiaSharp).

The Skia Rendering backend has a very cheap cost for creating UI elements, which makes it very efficient for large user interfaces.

#### Supported Platforms

Skia rendering is available on:

- macOS
- Linux
- Windows
- WebAssembly
- Android
- iOS

#### How Skia Rendering Works

- The entire UI is drawn on a Skia canvas
- There are **no native views**; all visuals are composed in Skia using vector graphics
- A minimal native shell (like a window or web canvas) hosts the Skia surface

> [!NOTE]
> Because it bypasses native UI components, Skia can offer pixel-perfect rendering and visual consistency. The same UI is offered by default, but platform-specific theming is possible using [Uno.Themes](xref:Uno.Themes.Overview).

You can find more details about using Skia rendering in your app [here](xref:Uno.Skia.Rendering).

### Native Rendering Targets

The native rendering is our oldest rendering backend, which uses the native components and APIs to render the UI.

Each platform has its own set of platform interactions, listed below, allowing for deep integration of native components into the visual tree.

#### Web (WebAssembly)

On WebAssembly, XAML elements are translated into HTML elements:

- Panels and layout containers are materialized as `<div>`
- Leaf controls like `TextBlock`, `Image` are materialized as semantic elements like `<p>`, `<img>`, etc.

This rendering integrates with CSS and DOM APIs, enabling UI behavior consistent with web standards.

#### iOS

- All `FrameworkElement` types are backed by native `UIView` instances.
- Controls like `Image` implicitly create native `UIImageView` subviews.
- Native input, layout, and accessibility features are utilized.

#### Android

- All `FrameworkElement` types inherit from native `ViewGroup`.
- Controls like `Image` wrap native `ImageView` instances.
- Leverages Android’s native rendering, accessibility, and gesture systems.

## On Windows using WinAppSDK

On Windows (the `net9.0-windows10.0.xxxxx` target framework) using [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/), the application isn't using Uno Platform at all. It's compiled just like a single-platform WinUI application, using Microsoft's own tooling.

The rest of this article explains how Uno Platform enables WinUI-compatible XAML and C# apps to run on **non-Windows platforms**.

## Uno Platform at Build Time

### Binaries

Uno Platform applications use [.NET 8+](https://learn.microsoft.com/dotnet/core/introduction) and run on all major platforms:

- **Mobile/Desktop**: via .NET for Mobile
- **Web**: via .NET for WebAssembly
- **Linux/macOS**: via standard .NET runtime

### XAML Processing

- XAML is parsed at build time using a Roslyn source generator.
- It’s compiled to C# to improve runtime performance.
- This is fully transparent to developers.

### Assets & Resources

- Images and other assets are transformed and copied to each platform’s expected structure.
- String resources (`.resw`) are converted into platform-specific formats.
- Learn more in [Working with Assets](xref:Uno.Features.Assets).

## Summary

Uno Platform enables a single codebase to target all major platforms by:

- Reproducing the WinUI API surface with `Uno.WinUI`
- Mapping XAML UI to native or Skia-based rendering pipelines
- Using .NET and MSBuild to compile apps for specific targets
- Automating asset, XAML, and resource handling for cross-platform compatibility

Whether you're building for mobile, desktop, or web, Uno Platform gives the flexibility to choose native or Skia rendering to best match the app's needs.
