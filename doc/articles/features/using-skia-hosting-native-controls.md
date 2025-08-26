---
uid: Uno.Skia.Embedding.Native
---

# Embedding Native Elements in Skia Apps

> [!NOTE]
> This document describes Skia renderer native embedding, for other platforms/renderers see the [native views](xref:Uno.Development.NativeViews) documentation.

In an Uno Platform app with a Skia renderer, i.e. using `net9.0-desktop` or adding `SkiaRenderer` to the `UnoFeatures` MSBuild property, you can embed native controls in your Skia app. This is useful if you want to use a native control for a specific task, for instance, to integrate an existing WPF control.

Each target platform has its own idea of a native element.

| Platform                        | Native element                                                                     | Description                                  |
|---------------------------------|------------------------------------------------------------------------------------|----------------------------------------------|
| Skia Desktop (Win32)            | Uno.UI.NativeElementHosting.Win32NativeWindow                                      | A native Windows window with a unique `Hwnd` |
| Skia Desktop (WPF)              | System.Windows.UIElement                                                           | A WPF control                                |
| Skia Desktop (X11)              | Uno.UI.NativeElementHosting.X11NativeWindow                                        | A native X11 window with a unique `XID`      |
| Skia Desktop (macOS)            | Not yet supported.                                                                 | Not yet supported as of Uno Platform 6.0     |
| WebAssembly with `SkiaRenderer` | [Uno.UI.NativeElementHosting.BrowserHtmlElement](xref:Uno.Interop.WasmJavaScript1) | An HTML element with a unique `id`.          |
| Android with `SkiaRenderer`     | Android.Views.View                                                                 | An Android view.                             |
| Apple UIKit with `SkiaRenderer` | UIKit.UIView                                                                       | An UIKit view.                               |

The app developer is responsible for creating the native element and internal checks make sure that only a supported native element on the running platform is used.

## Using embedded native controls

To embed a native element, you will need to set the native control as `Content` of a `ContentControl`, either via code or XAML. On desktop platforms, it's often more straightforward to create the native element via code since the parameters for creating the native element are not known ahead of time. For example, on Win32, you need to create a native Windows window first, get its `Hwnd` and then create a `Win32NativeWindow` instance with that `Hwnd` value.

Do not set a `ContentTemplate` or a `ContentTemplateSelector` on the `ContentControl`.

## Features

The layouting of native elements behaves mostly like regular `UIElement`s, using the native platform's measuring and arranging functions, e.g. `UIKit.UIView.SizeThatFits` on Apple UIKit, if they are present and defaulting to taking the entire available space on targets that don't have corresponding native measuring and arranging methods. On targets that don't have native measuring methods and expand to fill all available space, make sure that the wrapping `ContentControl` is not given infinite width or height when being measured, for example, by being put in a StackPanel. In those cases, limit the layout bounds by, for example, setting `MaxWidth`/`MaxHeight`/`Width`/`Height`.

> [!NOTE]
> `ContentControl` contains a `ContentPresenter` which hosts the actual native content. In the default `ContentControl` template, the `HorizontalAlignment` and `VerticalAlignment` of the `ContentPresenter` are bound to the `HorizontalContentAlignment` and `VerticalContentAlignment`, respectively, of the wrapping `ContentControl`, which are set by default to `Top` and `Left`, respectively. As a result, if your layout logic relies on the alignment/stretching of the `ContentControl`, you will likely want to set `<Horizontal|Vertical>ContentAlignment` to match `<Horizontal|Vertical>Alignment`.

Furthermore, native elements blend and overlap naturally with Uno controls and respect Z-axis ordering. For example, if you open a popup on top of a native element, the popup will show on top of the element. Native-managed blending is not limited to rectangular boundaries and elements clipped with arbitrary paths work as expected. For example, elements with rounded corners that are placed on a native element will not show up as rectangles but behave as usual with rounded corners.

Setting the `Opacity` of the wrapping `ContentControl` will also set the opacity of the hosted native element. Likewise, setting the `Visibility` of the wrapping `ContentControl` will flow to the native element and hide/show it.

> [!NOTE]
> As of Uno Platform 6.0, setting the opacity of native elements is not supported on X11.

## Limitations

The native control is rendered by the native windowing system and cannot be styled by Uno Platform styles.

While setting the transparency of native elements is supported, native elements don't alpha-blend. In other words, if a partially-transparent Uno control is placed on top of a native element, the native element will not be visible underneath the Uno control. Instead, the transparent area will behave as if the native element is not there and will only show managed Uno controls underneath.

Focus and pointer/keyboard input work as expected most of the time, but, depending on the platform, you might find some quirks with the way inputs are handled.

Placing Uno Controls outside of their layout bounds to be on top of native elements using `RenderTransform` or similar techniques will not clip correctly. The clipping of Uno controls is used to calculate which areas are painted by managed controls and how they overlap with native elements. If the clip bounds of a control are unbounded (i.e. the control isn't clipped at all), the clip bounds for managed-native overlapping purposes will be the layout rectangle of the control.
