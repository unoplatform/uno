---
uid: Uno.Controls.SKCanvasElement
---

## Introduction

When creating an Uno Platform application, developers might want to create elaborate 2D graphics using a library such as [Skia](https://skia.org) or [Cairo](https://www.cairographics.org), rather than using, for example, a simple [Canvas](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.canvas). To support this use case, SkiaSharp comes with an [SKXamlCanvas](https://learn.microsoft.com/dotnet/api/skiasharp.views.windows.skxamlcanvas) element that allows for drawing in an area using SkiaSharp.

On Uno Platform Skia Desktop targets, we can utilize the pre-existing internal Skia canvas used to render the application window instead of creating additional Skia surfaces. It is then possible to copy the resulting renderings to the application (e.g. using a [BitmapImage](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.imaging.bitmapimage)). This way, a lot of Skia functionally can be acquired "for free". For example, no additional additional packages are needed, and setup for hardware acceleration is not needed if the Uno application is already using OpenGL to render.

This functionality is exposed in two parts. `SkiaVisual` is a [Composition API Visual](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.visual) that gets an [SKCanvas](https://learn.microsoft.com/dotnet/api/skiasharp.skcanvas) to draw on and is almost completely unmanaged. For more streamlined usage, an `SKCanvasElement` is provided that internally wraps a `SkiaVisual` and can be used like any `FrameworkElement`, with support for sizing, clipping, RTL, etc. You should use `SKCanvasElement` for most scenarios. Only use a raw `SkiaVisual` if your use case is not covered by `SKCanvasElement`.

> [!IMPORTANT]
> This functionality is only available on Skia Desktop (`net8.0-desktop`) targets.

## SkiaVisual

A `SkiaVisual` is an abstract `Visual` that provides Uno Platform applications the ability to utilize SkiaSharp to draw directly on the Skia canvas that is used internally by Uno to draw the window. To use `SkiaVisual`, create a subclass of `SkiaVisual` and override the `RenderOverride` method.

```csharp
protected abstract void RenderOverride(SKCanvas canvas);
```

You can then add the `SkiaVisual` as a child visual of an element using [ElementCompositionPreview.SetElementChildVisual](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.elementcompositionpreview.setelementchildvisual#microsoft-ui-xaml-hosting-elementcompositionpreview-setelementchildvisual(microsoft-ui-xaml-uielement-microsoft-ui-composition-visual)).

Note that you will need to add your own logic to handle sizing and clipping.

When adding your drawing logic in `RenderOverride` on the provided canvas, you can assume that the origin is already translated so that `0,0` is the origin of the visual, not the entire window.

Additionally, `SkiaVisual` has an `Invalidate` method that can be used at any time to tell the Uno Platform runtime to redraw the visual, calling `RenderOverride` in the process.

## SKCanvasElement

`SKCanvasElement` is a ready-made `FrameworkElement` that creates an internal `SkiaVisual` and maintains its state as one would expect. To use `SKCanvasElement`, create a subclass of `SKCanvasElement` and override the `RenderOverride` method, which takes the canvas that will be drawn on and the clipping area inside the canvas. Drawing outside this area will be clipped.

```csharp
protected abstract void RenderOverride(SKCanvas canvas, Size area);
```

By default, `SKCanvasElement` takes all the available space given to it in the `Measure` cycle. If you want to customize how much space the element takes, you can override its `MeasureOverride` method.

Note that since `SKCanvasElement` takes as much space as it can, it's not allowed to place an `SKCanvasElement` inside a `StackPanel`, a `Grid` with `Auto` sizing, or any other element that provides its child(ren) with infinite space. To work around this, you can explicitly set the `Width` and/or `Height` of the `SKCanvasElement`.

`SKCanvasElement` also comes with a `MirroredWhenRightToLeftProperty`. If `true`, the drawing will be reflected horizontally when the `FlowDirection` of the `SKCanvasElement` is right-to-left. By default, this property is set to `false`, meaning that the drawing will be the same regardless of the `FlowDirection`.

## Full example

To see this in action, here's a complete sample that uses `SKCanvasElement` to draw 1 of 3 different drawings based on the value of a [Slider](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.slider). Note how you have to be careful with surrounding all the Skia-related logic in platform-specific guards. This is the case for both [xaml](platform-specific-xaml) and the [code-behind](platform-specific-csharp).

Xaml:

```xaml

```

Code-behind:

```csharp

```
