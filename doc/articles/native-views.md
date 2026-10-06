---
uid: Uno.Development.NativeViews
---

# Incorporating native views to the Uno visual tree

Uno Platform supports embedding a purely native view — one that does not come from Uno
Platform but is instead defined in a third-party library, via a binding, or in the native
framework itself — alongside the Skia-rendered visual tree. This is available on Android,
iOS, WebAssembly and on the Win32 and X11 desktop hosts.

> [!NOTE]
> Uno Platform 7.0 renders the UI exclusively with Skia. Native views are no longer part
> of the visual tree by inheritance; they are **embedded** as host-composited overlays
> through the Skia hosts. The legacy native-renderer mechanisms (`VisualTreeHelper.AdaptNative`,
> native XAML namespaces backing a native view tree) have been removed — see
> [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

## Adding JavaScript views in WebAssembly

On WebAssembly, [read this guide](xref:Uno.Interop.WasmJavaScript1) to learn how to embed
native (HTML/JS) views.

## Adding native views in Skia

On Skia targets, a native view is hosted by setting it as the `Content` of a `ContentControl`,
for example an `Android.Views.View` on Android or a `UIKit.UIView` on iOS.
[Read this guide](xref:Uno.Skia.Embedding.Native) to learn how.
