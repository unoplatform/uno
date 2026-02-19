---
uid: uno.features.renderer.skia
---

# The Skia Renderer

Available on iOS, Android, macOS, Windows, Linux and WebAssembly, based on the [Skia](https://skia.org) drawing library, the Skia Renderer is a cross-platform unified rendering component of Uno Platform which provides a single UI experience across all supported platforms.

The whole UI Visual Tree is drawn on an hardware accelerated canvas, using Metal, OpenGL, and WebGL where applicable. Unlike Native rendering, Skia doesn’t rely on platform UI components.

The Skia Rendering backend has a very cheap cost for creating UI elements, which makes it very efficient for large user interfaces.

Starting with Uno.Sdk 6.0, **it is the default rendering engine** when creating a project from the templates. On **WebAssembly (Wasm)**, **Android**, and **iOS**, you can opt in to use the native rendering engine instead.

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

You can use our [Visual Studio Wizard](xref:Uno.GettingStarted.UsingWizard) to create a new project. By default, the Wizard uses the Skia rendering engine, automatically setting up the necessary MSBuild properties and references for you. If you prefer to use native rendering instead, you can select it in the Wizard’s configuration options. You can find more details on how to use the Wizard here: [Creating a new project](xref:Uno.GettingStarted.UsingWizard).

> [!NOTE]
> If you're upgrading an existing project to Uno Platform 6.0, be sure to also check our migration guidance in [Migrating from previous releases](xref:Uno.Development.MigratingFromPreviousReleases).

## Using Skia rendering for existing apps

To enable Skia rendering in your Uno Platform project, you must opt in using MSBuild properties and features.

### Using Skia Desktop

On **macOS**, **Linux**, and **Windows**, using the `netX.0-desktop` target framework, Skia rendering is always used.

However, when using **WinAppSDK**, the **native rendering engine** is always used, regardless of whether Skia or Native is enabled in `UnoFeatures`.

> [!NOTE]
> Starting with Uno Platform 6.0, **Mac Catalyst** is no longer present in templates, and we encourage users to move to `netX.0-desktop`, which runs on macOS using Skia for rendering.

You can find more details in [Using the Skia Desktop](xref:Uno.Skia.Desktop).

### Upgrading to use Skia for iOS, Android, and WebAssembly

> [!TIP]
> If your project was created before Uno Platform 6.0 and you want to enable Skia rendering, [follow the upgrade guide](xref:Uno.Development.MigratingToUno6).

## Limitations

Using Skia rendering might have some limitations compared to native rendering. Some of the known limitations include:

- **Accessibility support**: Since Skia doesn't rely on native controls, accessibility tools (e.g., screen readers) are a work in progress. We're actively improving accessibility support in future releases.
- **Text rendering differences**: Font rendering may not match platform-specific expectations due to differences in text shaping and anti-aliasing.
- **IME support**: This portion of input support is also a work in progress, expect improvements in upcoming releases.
- **Limited hardware acceleration on some platforms**: Depending on the platform, Skia may fall back to software rendering, affecting the overall performance.
- Skia Rendering on WebAssembly is only supported on .NET 9 and later.
- Removing `SkiaRenderer` from `UnoFeatures` inside the project's `.csproj` file to target native rendering, then setting it back after a new build will raise a runtime Exception. You will need to remove `bin` and `obj` folders, and clear site data of the browser app in order to avoid encountering this exception.

Skia rendering is best suited for cross-platform scenarios where a unified appearance and customized graphics are key. Some native integration scenarios may not yet be supported. If you encounter any of such scenarios, make sure to let it be known by [opening an issue](https://github.com/unoplatform/uno/issues).

## Architecture

In order to accommodate the inclusion of Skia rendering for all platforms, the Uno Platform internal structure uses two layers of "bait-and-switch" of reference assemblies.

### Publish-time switching

When building in the `SkiaRenderer` node, an application compiles against "reference" versions of `Uno.UI`, `Uno.UI.Dispatching`, `Uno.UI.Composition`, `Uno`, and `Uno.Foundation`. When the application is being packaged, `Uno.UI` and `Uno.UI.Composition` are switched to the Skia-compatible versions. The `Uno.UI.Dispatching`, `Uno`, and `Uno.Foundation` are switched to their corresponding target platform versions.

By doing so, any use of the APIs provided by `Uno.UI.Dispatching`, `Uno`, and `Uno.Foundation` is automatically redirected to the proper platform support, for instance, redirecting `GeoLocator` to use the proper APIs provided by the underlying platform.

### Implications for iOS/Android class libraries

At this time, NuGet packages that provide UI features on iOS/Android may not be consumed by both native and Skia renderer projects under certain conditions.

The reason for this limitation is caused by the fact that Native renderers expose a different API set for `Microsoft.UI.Xaml` types, therefore causing incompatibility issues when apps use such a library.

Here are the different scenarios:

- Given a library that uses `net10.0-ios` or `net10.0-android`, which does not have the `SkiaRenderer` set as an `UnoFeature`, but uses platform conditional code with `#if` blocks:

  - This package is only usable by both native and Skia apps if it also provides a `net10.0` TFM. In this case, for a native app, nothing changes.
  - For a `SkiaRenderer` enabled app, the `net10.0` variant of the library will be used and will not offer iOS/Android specific conditional code, but any code that uses Uno Platform provided APIs will work properly.

- Given a library that uses `net10.0-ios` or `net10.0-android`, which does have the `SkiaRenderer` set as an `UnoFeature`, but uses platform conditional code with `#if` blocks:

  - This package is only usable by Skia-enabled apps.
