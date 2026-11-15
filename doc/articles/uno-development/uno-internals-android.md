---
uid: Uno.Contributing.Android
---

# How Uno works on Android

Android is supported through the Skia rendering backend. The native Android target (where `UIElement` inherited from `ViewGroup` and layout was driven by the Android layout cycle) was removed in Uno Platform 7.0. The UI is now drawn with the same Skia-based rendering pipeline as the other platforms, and Uno runs measure and arrange itself.

The Android host lives in [`Uno.UI.Runtime.Android`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI.Runtime.Android). `ApplicationActivity` hosts the window, and the views in its `Rendering` folder (`UnoCanvasView`, `UnoVulkanView`) present the frames drawn by Skia. Native Android views can still be embedded alongside the Skia-rendered tree, see [Embedding Native Elements in Skia Apps](xref:Uno.Skia.Embedding.Native).

For details on how the Skia backend works, see the [overview article](uno-internals-overview.md).
