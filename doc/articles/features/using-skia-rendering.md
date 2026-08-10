---
uid: uno.features.renderer.skia
---

# The Skia Renderer

Available on iOS, Android, macOS, Windows, Linux and WebAssembly, based on the [Skia](https://skia.org) drawing library, the Skia Renderer is a cross-platform unified rendering component of Uno Platform which provides a single UI experience across all supported platforms.

The whole UI Visual Tree is drawn on an hardware accelerated canvas, using Metal, OpenGL, [Vulkan](xref:Uno.Skia.Vulkan), and WebGL where applicable. Unlike Native rendering, Skia doesn’t rely on platform UI components.

The Skia Rendering backend has a very cheap cost for creating UI elements, which makes it very efficient for large user interfaces.

Starting with Uno.Sdk 6.0, **it is the default rendering engine** when creating a project from the templates. Starting with Uno Platform 7.0, it is the **only** rendering engine: the native renderers have been removed, and the `SkiaRenderer` `UnoFeature` is implied on every target and kept as a no-op for backwards compatibility.

This renderer supports [integrating native views](xref:Uno.Skia.Embedding.Native).

> [!NOTE]
> The **WinAppSDK** target is not provided by Uno Platform directly, so it only offers the **native rendering mode**.

## How Skia Rendering Works

- The entire UI is drawn on a Skia canvas
- There are **no native views**; all visuals are composed in Skia using vector graphics
- A minimal native shell (like a window or web canvas) hosts the Skia surface

As the Skia Renderer bypasses native UI components, Skia can offer pixel-perfect rendering and visual consistency. The same UI is offered by default, but platform-specific theming is possible using [Uno.Themes](xref:Uno.Themes.Overview).

## Benefits

> [!TIP]
> If you are building a custom drawing application, charts, or games in Uno Platform, Skia can offer more flexibility and uniform visuals across platforms.

- **Consistent visuals**: Skia ensures pixel-perfect rendering across all supported platforms, making it ideal for applications where precise control over appearance is critical.
- **Custom drawing**: Ideal for apps requiring advanced graphics, custom controls, or canvas-based rendering—Skia gives you low-level drawing access, such as with the [SKCanvasElement](xref:Uno.Controls.SKCanvasElement).
- **Unified rendering pipeline**: Unlike native rendering, which varies by platform, Skia uses a single rendering backend, reducing platform-specific variations.
- **Improved rendering performance on desktop**: On platforms like Linux/macOS, Skia is often faster and more efficient than native alternatives.
- **Access to the full Composition API**: The Skia renderer provides access to the full [Composition API access for richer custom rendering](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/composition).
- **Better control over visual updates**: You can fine-tune repainting behavior for animations, games, or dynamic content using Skia’s immediate mode rendering.
- **Smaller dependency surface**: By avoiding native UI components, Skia can simplify deployment—especially in environments like Wasm or containerized desktop apps.

## Using Skia rendering for new apps

You can use our [Visual Studio Wizard](xref:Uno.GettingStarted.UsingWizard) to create a new project. The Wizard uses the Skia rendering engine, automatically setting up the necessary MSBuild properties and references for you. You can find more details on how to use the Wizard here: [Creating a new project](xref:Uno.GettingStarted.UsingWizard).

> [!NOTE]
> If you're upgrading an existing project to Uno Platform 6.0, be sure to also check our migration guidance in [Migrating from previous releases](xref:Uno.Development.MigratingFromPreviousReleases).

## Using Skia rendering for existing apps

Skia rendering requires no opt-in: it is used on every Uno Platform target. There is no MSBuild property or `UnoFeature` to enable or disable it.

### Using Skia Desktop

On **macOS**, **Linux**, and **Windows**, using the `netX.0-desktop` target framework, Skia rendering is always used.

However, when using **WinAppSDK**, the **native WinUI rendering engine** is always used, since that target is not provided by Uno Platform.

> [!NOTE]
> Starting with Uno Platform 6.0, **Mac Catalyst** is no longer present in templates, and we encourage users to move to `netX.0-desktop`, which runs on macOS using Skia for rendering.

You can find more details in [Using the Skia Desktop](xref:Uno.Skia.Desktop).

### Upgrading to use Skia for iOS, Android, and WebAssembly

> [!TIP]
> If your project still relies on native rendering, [follow the Uno Platform 7.0 migration guide](xref:Uno.Development.MigratingToUno7). For projects created before Uno Platform 6.0, [start with the 6.0 upgrade guide](xref:Uno.Development.MigratingToUno6) to move to the Uno.Sdk single-project model.

## Limitations

Using Skia rendering might have some limitations compared to native rendering. Some of the known limitations include:

- **Accessibility support**: Since Skia doesn't rely on native controls, accessibility tools (e.g., screen readers) are a work in progress. We're actively improving accessibility support in future releases.
- **Text rendering differences**: Font rendering may not match platform-specific expectations due to differences in text shaping and anti-aliasing.
- **IME support**: This portion of input support is also a work in progress, expect improvements in upcoming releases.
- **Limited hardware acceleration on some platforms**: Depending on the platform, Skia may fall back to software rendering, affecting the overall performance.
- Skia Rendering on WebAssembly is only supported on .NET 9 and later.

Skia rendering is best suited for cross-platform scenarios where a unified appearance and customized graphics are key. Some native integration scenarios may not yet be supported. If you encounter any of such scenarios, make sure to let it be known by [opening an issue](https://github.com/unoplatform/uno/issues).

## Architecture

In order to provide platform-specific WinRT APIs from a single UI assembly, the Uno Platform internal structure uses a "bait-and-switch" of reference assemblies for its WinRT layer, while the UI layer uses the Skia build directly.

### Publish-time switching

The UI assemblies, `Uno.UI` and `Uno.UI.Composition`, are the Skia build: an application compiles against exactly the assemblies it runs against.

The WinRT assemblies, `Uno.UI.Dispatching`, `Uno`, and `Uno.Foundation`, still use the switch. An application compiles against a "reference" version of each, which carries the union of the API surface across platforms, and when the application is packaged each is switched to the version matching the target platform.

By doing so, any use of the APIs provided by `Uno.UI.Dispatching`, `Uno`, and `Uno.Foundation` is automatically redirected to the proper platform support, for instance, redirecting `GeoLocator` to use the proper APIs provided by the underlying platform.

### Implications for iOS/Android class libraries

A library that targets `net10.0-ios` or `net10.0-android` and uses platform conditional code with `#if` blocks needs to also provide a `net10.0` TFM to be consumable by an application. The `net10.0` variant is the one that gets used: it does not offer iOS/Android specific conditional code, but any code that uses Uno Platform provided APIs works properly.

> [!TIP]
> For code that needs to behave differently per platform, prefer runtime branching from a single `net10.0` target framework, using [`OperatingSystem.IsIOS()`](https://learn.microsoft.com/dotnet/api/system.operatingsystem.isios), `OperatingSystem.IsAndroid()`, and the other `OperatingSystem.IsXXX` methods. See [Platform-specific C# code](xref:Uno.Development.PlatformSpecificCSharp).
>
> Runtime branching cannot reach platform SDK types such as `UIKit` or `Android.*`, which are not available from a `net10.0` target framework. When those are genuinely required, expose an interface from the `net10.0` library and inject the platform implementation from the application head, which does target `net10.0-ios`/`net10.0-android`.
