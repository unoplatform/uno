---
uid: Uno.Controls.SKCanvasElement
---

# SKCanvasElement

When creating an Uno Platform application, developers might want to create elaborate 2D graphics using a library such as [Skia](https://skia.org) or [Cairo](https://www.cairographics.org), rather than using, for example, a simple [Canvas](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.canvas). To support this use case, SkiaSharp comes with an [SKXamlCanvas](https://learn.microsoft.com/dotnet/api/skiasharp.views.windows.skxamlcanvas) element that allows for drawing in an area using SkiaSharp.

On Uno Platform targets with Skia rendering, we can utilize the pre-existing internal Skia canvas used to render the application window instead of creating additional Skia surfaces. Unlike `SKXamlCanvas` which doesn't support hardware acceleration on Skia targets yet, hardware acceleration comes out of the box if the Uno application is already using OpenGL to render. Moreover, `SKXamlCanvas` has to make additional buffer copying, which can be skipped with this implementation.

> [!IMPORTANT]
> . The `Uno.WinUI.Graphics2DSK` package which provides the `SKCanvasElement` class is referenced for you by Uno.Sdk automatically when the `Skia` or `SkiaRenderer` features are present, and is always referenced on the netX.0-desktop target. However, the class is only supported (i.e. `SKCanvasElement.IsSupportedOnCurrentPlatform` returns true) on the netX.0-desktop target and on other targets that use skia rendering, for example netX.0-android with the `SkiaRenderer` feature.

`SKCanvasElement` is an abstract `FrameworkElement` for 2D drawing with Skia. To use `SKCanvasElement`, create a subclass of `SKCanvasElement` and override the `RenderOverride` method, which takes the canvas that will be drawn on and the clipping area inside the canvas.

```csharp
protected abstract void RenderOverride(SKCanvas canvas, Size area);
```

When adding your drawing logic in `RenderOverride` on the provided canvas, you can assume that the origin is already translated so that `0,0` is the origin of the element, not the entire window. Drawing outside this area will be clipped.

Additionally, `SKCanvasElement` has an `Invalidate` method that invalidates the `SKCanvasElement` and triggers a redraw. The drawing of the `SKCanvasElement` is often cached and will not be updated unless `Invalidate` is called.

Since `SKCanvasElement` is just a FrameworkElement, controlling the dimensions of the drawing area is done by manipulating the layout of the element, e.g. by overriding MeasureOverride and ArrangeOverride.

Finally, since `SKCanvasElement` makes use of the Skia-based rendering infrastructure and is therefore only supported when skia rendering is used, there's a static method that provides a check at runtime for whether `SKCanvasElement` can function on the current platform.

```csharp
public static bool IsSupportedOnCurrentPlatform();
```

Attempting to create an `SKCanvasElement` instance when `IsSupportedOnCurrentPlatform` returns false throws an exception.

## Full example

For a complete example that showcases how to work with `SKCanvasElement`, see [this sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/SKCanvasElementShowcase) in the Uno.Samples repository

## WinAppSDK Specifics

When using the SKCanvasElement and running on WinAppSDK, make sure to create an `x64` or `ARM64` configuration:

- In the Visual Studio configuration manager, create an `x64` or `ARM64` solution configuration
- Assign it to the Uno Platform project
- Debug your application using the configuration relevant to your current environment
