---
uid: Uno.Skia.Rendering
---

# Using the Skia Rendering

The Skia rendering engine is available for all platforms supported by Uno Platform. It is the default rendering engine for **macOS** and **Linux**, and it can also be used as an alternative rendering engine on **Windows**, **WebAssembly (Wasm)**, **Android**, and **iOS**. Skia provides a consistent rendering pipeline across platforms and is often used to achieve improved performance or visual fidelity, particularly in scenarios where the default platform rendering may be lacking.

## Using Skia rendering for new apps

You can use our [Visual Studio Wizard](xref:Uno.GettingStarted.UsingWizard) and enable the Skia rendering engine when creating a new project. This will set up the necessary MSBuild properties and references for you. You can find more details on how to use the wizard here: [Creating a new project](xref:Uno.GettingStarted.UsingWizard).

## Using Skia rendering for existing apps

To enable Skia rendering in your Uno Platform project, you must opt in using MSBuild properties and features.

### Using Skia Desktop

On desktop platforms (Windows, macOS, Linux), Skia is used by default on **macOS** and **Linux** targets. On **Windows**, you can opt into using Skia instead of the default WinUI-based rendering.

You can find more details in [Using the Skia Desktop](xref:Uno.Skia.Desktop).

### Using Skia Mobile/Wasm

1. To enable Skia rendering on **WebAssembly**, **Android**, and **iOS**, set the following in your `.csproj`:

```diff
<UnoFeatures>
   ...
   Serialization;
   Localization;
   Navigation;
+  MauiEmbedding;
</UnoFeatures>
```

1. Then, include the platform-specific flags:

```xml
<PropertyGroup>
  <UseSkiaRendering>true</UseSkiaRendering>

  <IsSkiaWasm Condition="'$(UseSkiaRendering)'=='true' AND $(IsBrowserWasm)">true</IsSkiaWasm>
  <IsSkiaWasm Condition="'$(IsSkiaWasm)'==''">false</IsSkiaWasm>

  <IsSkiaAndroid Condition="'$(UseSkiaRendering)'=='true' AND $(IsAndroid)">true</IsSkiaAndroid>
  <IsSkiaAndroid Condition="'$(IsSkiaAndroid)'==''">false</IsSkiaAndroid>

  <IsSkiaUIKit Condition="'$(UseSkiaRendering)'=='true' AND $(IsIOSOrCatalyst)">true</IsSkiaUIKit>
  <IsSkiaUIKit Condition="'$(IsSkiaUIKit)'==''">false</IsSkiaUIKit>
</PropertyGroup>
```

1. You need to add a reference for `Uno.Extensions.Logging.WebAssembly.Console` to your project. This is required for the Skia rendering engine to work correctly on WebAssembly. You can do this by adding the following line to your `.csproj` file:

```xml
<PackageReference Include="Uno.Extensions.Logging.WebAssembly.Console" />
```

And then add the following line to your `Directory.Packages.props` file:

```xml
<PackageVersion Include="Uno.Extensions.Logging.WebAssembly.Console" Version="XX" />
```

Make sure to replace `XX` with the latest version of the package, which you can find on [Nuget package page](https://www.nuget.org/packages/Uno.Extensions.Logging.WebAssembly.Console/#versions-body-tab).

1. Restart the IDE to ensure the changes are properly applied.

#### Android

- On Android, you will need to remove the `ImageSource` in `Main.Android.cs` and replace it with the following:

```diff
private static void ConfigureUniversalImageLoader()
{
    ...
-   ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
}
```

> [!Warning]
> The Skia backend bypasses native controls and renders everything via SkiaCanvas, which can affect behavior for accessibility and platform-specific UI expectations.

## Benefits

> [!TIP]
> If you're building a custom drawing application, charts, or games in Uno Platform, Skia can offer more flexibility and uniform visuals across platforms.

- **Consistent visuals**: Skia ensures pixel-perfect rendering across all supported platforms, making it ideal for applications where precise control over appearance is critical.
- **Custom drawing**: Ideal for apps requiring advanced graphics, custom controls, or canvas-based rendering—Skia gives you low-level drawing access.
- **Unified rendering pipeline**: Unlike native rendering, which varies by platform, Skia uses a single rendering backend, reducing platform-specific inconsistencies.
- **Improved rendering performance on desktop**: On platforms like Linux/macOS, Skia is often faster and more efficient than native alternatives.
- **Better control over visual updates**: You can fine-tune repainting behavior for animations, games, or dynamic content using Skia’s immediate mode rendering.
- **Smaller dependency surface**: By avoiding native UI components, Skia can simplify deployment—especially in environments like Wasm or containerized desktop apps.

## Limitations

Using Skia rendering might have some limitations compared to the native rendering. Some of the known limitations include:

- **No native control integration**: Skia draws all UI elements as vector graphics, which means platform-native controls (like a native text box or button) are not used. This can affect accessibility and input handling.
- **Accessibility support**: Since Skia doesn't rely on native controls, accessibility tools (e.g., screen readers) may not function as expected.
- **Performance**: While Skia is fast for drawing vector graphics, it may not outperform native rendering in all cases—especially on lower-end mobile devices or in scenarios with heavy UI updates.
- **Text rendering differences**: Font rendering may not match platform-specific expectations due to differences in text shaping and anti-aliasing.
- **Limited hardware acceleration on some platforms**: Depending on the platform, Skia may fall back to CPU rendering, affecting performance.

Skia rendering is best suited for cross-platform scenarios where a unified appearance is more critical than deep integration with native platform features.
