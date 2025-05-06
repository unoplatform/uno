---
uid: Uno.Skia.Rendering
---

# Using the Skia Rendering

The Skia rendering engine is available for all Uno Platform targets. **Starting with Uno.Sdk 6.0, it is the default rendering engine when creating a project from the templates.** On **WebAssembly (Wasm)**, **Android**, and **iOS**, you can opt in to use the native rendering engine instead. The **WinAppSDK** target is not provided by Uno Platform directly, so it only offers the **native rendering mode**.

Skia provides a consistent rendering pipeline across platforms and is often used to achieve improved performance or visual fidelity, particularly in scenarios where the default platform rendering may be lacking. Under the hood, the Uno Platform's Skia rendering engine is powered by SkiaSharp, a .NET binding to the Skia graphics library.

## Using Skia rendering for new apps

You can use our [Visual Studio Wizard](xref:Uno.GettingStarted.UsingWizard) to create a new project. By default, the Wizard uses the Skia rendering engine, automatically setting up the necessary MSBuild properties and references for you. If you prefer to use native rendering instead, you can select it in the Wizard’s configuration options. You can find more details on how to use the Wizard here: [Creating a new project](xref:Uno.GettingStarted.UsingWizard).

> [!NOTE]
> If you're upgrading an existing project to Uno Platform 6.0, be sure to also check our migration guidance in [Migrating from previous releases](xref:Uno.MigratingFromPreviousReleases).

## Using Skia rendering for existing apps

To enable Skia rendering in your Uno Platform project, you must opt in using MSBuild properties and features.

### Using Skia Desktop

On **macOS**, **Linux**, and **Windows**, using the `netX.0-desktop` target framework, Skia rendering is always used.

However, when using **WinAppSDK**, the **native rendering engine** is always used, regardless of whether Skia or Native is enabled in `UnoFeatures`.

> [!NOTE] Starting with Uno Platform 6.0, **Mac Catalyst** is no longer present in templates, and we encourage users to move to `netX.0-desktop`, which runs on macOS using Skia for rendering.

You can find more details in [Using the Skia Desktop](xref:Uno.Skia.Desktop).

### Upgrading to use Skia for iOS, Android, and WebAssembly

> [!TIP]
> If your project was created before Uno Platform 6.0 and you want to enable Skia rendering, [follow the upgrade guide](xref:Uno.Development.MigratingToUno6).

## Benefits

> [!TIP]
> If you are building a custom drawing application, charts, or games in Uno Platform, Skia can offer more flexibility and uniform visuals across platforms.

- **Consistent visuals**: Skia ensures pixel-perfect rendering across all supported platforms, making it ideal for applications where precise control over appearance is critical.
- **Custom drawing**: Ideal for apps requiring advanced graphics, custom controls, or canvas-based rendering—Skia gives you low-level drawing access, such as with the [SKCanvasElement](xref:Uno.Controls.SKCanvasElement).
- **Unified rendering pipeline**: Unlike native rendering, which varies by platform, Skia uses a single rendering backend, reducing platform-specific variations.
- **Improved rendering performance on desktop**: On platforms like Linux/macOS, Skia is often faster and more efficient than native alternatives.
- **Access to the full Composition API**: The Skia renderer provides access to the full [Composition API access for richer custom rendering](ttps://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/composition).
- **Better control over visual updates**: You can fine-tune repainting behavior for animations, games, or dynamic content using Skia’s immediate mode rendering.
- **Smaller dependency surface**: By avoiding native UI components, Skia can simplify deployment—especially in environments like Wasm or containerized desktop apps.

## Limitations

Using Skia rendering might have some limitations compared to native rendering. Some of the known limitations include:

- **Accessibility support**: Since Skia doesn't rely on native controls, accessibility tools (e.g., screen readers) are a work in progress. We're actively improving accessibility support in future releases.
- **Text rendering differences**: Font rendering may not match platform-specific expectations due to differences in text shaping and anti-aliasing.
- **IME support**: This portion of input support is also a work in progress, expect improvements in upcoming releases.
- **Limited hardware acceleration on some platforms**: Depending on the platform, Skia may fall back to software rendering, affecting the overall performance.

Skia rendering is best suited for cross-platform scenarios where a unified appearance and customized graphics are key. Some native integration scenarios may not yet be supported. If you encounter any of such scenarios, make sure to let it be known by [opening an issue](https://github.com/unoplatform/uno/issues).
