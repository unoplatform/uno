---
uid: Uno.Development.HowItWorks
---

# How Uno Platform Works

How does Uno Platform make the same application code run on all platforms?

## Uno Platform at Runtime

The [`Uno.WinUI` NuGet package](https://www.nuget.org/packages/Uno.WinUI/) and [`Uno.WinRT` NuGet package](xref:uno.features.uno.winrt) implement the entire WinUI and WinRT API surface across platforms. This includes:

- Namespaces like `Microsoft.UI` and related UI APIs
- The support for cross-platform non-UI APIs, such as file system access, devices and sensors, pickers, etc...

> [!NOTE]
> As the API surface is very large, some parts are included but not implemented. These features are marked with the [`Uno.NotImplementedAttribute`] attribute, and a code analyzer is included with the Uno.WinUI package will generate a warning for any such features that are referenced. You can see a complete list of supported APIs in the [implemented views documentation](implemented-views.md).

## Rendering the UI

As a developer, you write your UI using the standard WinUI-based XAML. Under the hood, Uno Platform draws this visual tree with Skia on every platform.

For every platform, the public API - panels, control templates, measurement and arranging, etc - is the same. However, it's sometimes useful to know what's going on at render-time, for example when [embedding native views inside XAML views](xref:Uno.Development.NativeViews) on a platform-specific basis.

Uno Platform uses Skia to draw all the app's pixels on the screen. Starting with Uno Platform 7.0, it is the only renderer: the native renderer, which used the native platform APIs to render on the screen, has been removed.

### Skia Rendering

Available on iOS, Android, macOS, Windows, Linux and WebAssembly, based on the [Skia](https://skia.org) drawing library, the Skia Renderer is a cross-platform unified rendering component of Uno Platform which provides a single UI experience across all supported platforms.

The whole UI Visual Tree is drawn on an hardware accelerated canvas, using Metal, OpenGL, and WebGL where applicable. Skia doesn’t rely on platform UI components, but native views can still be [embedded alongside the Skia-rendered tree](xref:Uno.Skia.Embedding.Native).

The Skia Rendering backend has a very cheap cost for creating UI elements, which makes it very efficient for large user interfaces.

For more information, see our documentation on the [Skia renderer](xref:uno.features.renderer.skia).

## Windows using WinAppSDK

On Windows (the `net10.0-windows10.0.xxxxx` target framework) using [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/), the application isn't using Uno Platform at all. It's compiled just like a single-platform WinUI application, using Microsoft's own tooling.

## Uno Platform at Build Time

### Binaries

Uno Platform applications use [.NET 9 or later](https://learn.microsoft.com/dotnet/core/introduction) and run on all major platforms:

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
