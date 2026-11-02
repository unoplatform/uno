---
uid: uno.features.renderer.webgpu
---

# WebGPU Rendering Backend

> [!IMPORTANT]
> The WebGPU rendering backend is experimental. [Skia](xref:uno.features.renderer.skia) remains the default renderer, and an app uses WebGPU only when it selects it on its host builder.

The WebGPU backend is an alternative to the SkiaSharp backend for drawing the UI. It renders through WebGPU: the `wgpu-native` library on native targets, and the browser's WebGPU support on WebAssembly.

## Adding the backend

Add the `WebGpu` [UnoFeature](xref:Uno.Features.Uno.Sdk) to the app project:

```xml
<UnoFeatures>Skia;WebGpu</UnoFeatures>
```

`WebGpu` counts as a renderer feature, so listing it stops `Skia` from being implied. List both, as above, to keep SkiaSharp in the app, which the fallback described below needs. Listing `WebGpu` without `Skia` produces an app with no SkiaSharp (see [Apps without SkiaSharp](#apps-without-skiasharp)).

The `WebGpuCanvas` feature, which adds [`WebGpuCanvasElement`](xref:Uno.Controls.WebGpuCanvasElement), also adds the WebGPU backend. It doesn't count as a renderer feature, so `Skia` is still implied next to it.

## Selecting WebGPU on the host builder

Referencing the backend doesn't turn it on: wherever SkiaSharp is present, Skia renders by default. To render with WebGPU, register it with `GraphicsBackend(...)` on the host builder:

```csharp
using Uno.UI.Composition.Skia;
using Uno.UI.Composition.WebGpu;
using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .GraphicsBackend(WebGpuBackend.CreateGraphicsProvider())
    .GraphicsBackend(SkiaBackend.CreateGraphicsProvider())
    .UseX11()
    .UseLinuxFrameBuffer()
    .UseMacOS()
    .UseWin32()
    .Build();

host.Run();
```

The same two calls apply to the host builder of each target, for example before `.UseAndroid()`, `.UseAppleUIKit()` or `.UseWebAssembly()`.

## Fallback

Each `GraphicsBackend(...)` call adds a backend in order of preference. When a window is created, the backends are tried in that order, and the first one that initializes is used.

Registering any backend replaces the implicit Skia default. An app that registers only WebGPU therefore has no fallback: on a device or browser where WebGPU can't be initialized, startup fails with an `InvalidOperationException` listing each attempt. To fall back to Skia, register it after WebGPU, as in the sample above.

On Windows (Win32) and Linux (X11), the host builder's backend options also apply to WebGPU. `DisableRenderingBackends(Win32RenderingBackend.WebGpu)` or `DisableRenderingBackends(X11RenderingBackend.WebGpu)` keeps an app that registers WebGPU on the Skia backends, and forcing any other backend with `ForceRenderingBackend(...)` excludes WebGPU. See [Vulkan Rendering Backend](xref:Uno.Skia.Vulkan).

## Supported hosts

| Host | WebGPU |
|------|--------|
| Windows (Win32) | Yes |
| Linux (X11) | Yes |
| macOS | Yes, unless the host renders in software (no Metal, or `FeatureConfiguration.Rendering.UseMetalOnMacOS = false`) |
| Android | Yes |
| iOS | Yes |
| WebAssembly | Yes, in browsers with WebGPU support, except when software rendering is forced |
| tvOS | No: the backend ships no native WebGPU library for it |
| Linux framebuffer | No |
| Windows App SDK | No: this target renders with WinUI |

Where WebGPU isn't available, an app that registered Skia after it renders with Skia.

## Apps without SkiaSharp

When `UnoFeatures` lists `WebGpu` without `Skia`, the app references neither SkiaSharp nor its native libraries. WebGPU is then the only renderer, and it is used without a `GraphicsBackend(...)` call. Fonts, images, geometry, [SVG](xref:Uno.Features.SVG) and [Lottie](xref:Uno.Features.Lottie) use the managed engines built into Uno Platform (`Uno.UI.Composition.Managed.ManagedBackend`) instead of their SkiaSharp implementations.

Such an app has no fallback renderer, and can't use SkiaSharp itself, for example to draw with [`SKCanvasElement`](xref:Uno.Controls.SKCanvasElement). An app that needs either lists `Skia;WebGpu`.

To configure the managed font provider, register it with `FontProvider(ManagedBackend.CreateFontProvider(...))` on the host builder. Its `bundledDefaultFont` parameter sets the default face on platforms whose system fonts can't be enumerated (iOS and WebAssembly), and `enableFallback` is described in [Custom fonts](xref:Uno.Features.CustomFonts#disabling-fallback).

## Diagnostic logging

To see which backend was selected, enable information-level logging for the negotiation:

```csharp
builder.AddFilter("Uno.UI.Composition.Drawing", LogLevel.Information);
```

The selected backend is logged as:

```text
Graphics backend '<backend>' won negotiation on context kind '<kind>'.
```

## See also

- [Skia Renderer](xref:uno.features.renderer.skia)
- [Vulkan Rendering Backend](xref:Uno.Skia.Vulkan)
- [WebGpuCanvasElement](xref:Uno.Controls.WebGpuCanvasElement)
