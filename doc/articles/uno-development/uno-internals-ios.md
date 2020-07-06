# How Uno works on iOS

This article explores iOS-specific details of Uno's internals, with a focus on information that's useful for contributors to Uno. For an overview of how Uno works on all platforms, see [this article](uno-internals-overview.md).

## UIElement inherits from UIView

`UIElement` in Uno is a native view on iOS, inheriting from the general `UIView` type. To elaborate, `UIElement`'s base classes are the following:
`UIKit.UIView` → `Uno.UI.Controls.BindableUIView` → `Windows.UI.Xaml.UIElement`

Recall that `UIElement` implements `DependencyObject` [as an interface](uno-internals-overview.md) in Uno.

## Layouting

Uno's measure and arrange logic is triggered from the native iOS layout cycle. For a schematic of the control flow, see [Layouting in iOS](Uno-UI-Layouting-iOS.md).
