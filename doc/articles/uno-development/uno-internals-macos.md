# How Uno works on macOS

This article explores macOS-specific details of Uno's internals, with a focus on information that's useful for contributors to Uno. For an overview of how Uno works on all platforms, see [this article](uno-internals-overview.md).

## UIElement inherits from NSView

`UIElement` in Uno is a native view on macOS, inheriting from the general `NSView` type. To elaborate, `UIElement`'s base classes are the following:
`AppKit.NSView` → `Uno.UI.Controls.BindableNSView` → `Windows.UI.Xaml.UIElement`

Recall that `UIElement` implements `DependencyObect` [as an interface](uno-internals-overview.md) in Uno.

## Similarities with iOS

Since the iOS and macOS UI frameworks bear substantial resemblances, the same or very similar code can frequently be used to do the same thing on both platforms, which Uno takes advantage of where possible. Partial class definitions that are shared between iOS and macOS can be identified by the `*.iOSmacOS.cs` suffix. Since Uno's iOS support predates its macOS support, in some cases internal methods have been added to make macOS' API appear still closer to that of iOS and make code reuse easier, such as the `SizeThatFits()` method or the `SetNeedsLayout()` extension method.