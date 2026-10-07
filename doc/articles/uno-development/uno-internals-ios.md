---
uid: Uno.Contributing.iOS
---

# How Uno works on iOS

iOS is supported through the Skia rendering backend. The native iOS target (where `UIElement` inherited from `UIView` and layout was driven by the UIKit layout cycle) was removed in Uno Platform 7.0. The UI is now drawn with the same Skia-based rendering pipeline as the other platforms, and Uno runs measure and arrange itself.

The iOS host lives in [`Uno.UI.Runtime.AppleUIKit`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI.Runtime.AppleUIKit). `UnoUIApplicationDelegate` handles the application lifecycle, and `UnoMetalView` presents the frames drawn by Skia. Native UIKit views can still be embedded alongside the Skia-rendered tree, see [Embedding Native Elements in Skia Apps](xref:Uno.Skia.Embedding.Native).

For details on how the Skia backend works, see the [overview article](uno-internals-overview.md).
