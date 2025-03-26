---
uid: Uno.Contributing.Android
---

# How Uno works on Android

This article explores Android-specific details of Uno's internals, with a focus on information that's useful for contributors to Uno. For an overview of how Uno works on all platforms, see [this article](xref:Uno.Contributing.Overview).

Some particulars of Android:

## Some classes are written in Java

Several base classes and helper classes are written in native Java code. These are located in [Uno.UI.BindingHelper.Android](../../../src/Uno.UI.BindingHelper.Android/Uno/UI).

The Xamarin.Android framework gives complete access to the Android API from managed C#, so why write anything in Java? The reason is performance. The interop between Java and C# can be costly, and instrumented profiling identified that certain virtual methods when overridden in C# and called in heavily-used paths (eg measure and arrange) imposed a measurable performance penalty. Over time, these 'hot' methods have been lowered to Java classes, particularly the `UnoViewGroup` class.

The `Uno.UI.BindingHelper.Android` project builds these Java types, and wraps them in a [Xamarin binding library](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/), making them available via C#.

## UIElement inherits from ViewGroup

`UIElement` in Uno is a native view on Android, inheriting from the general `ViewGroup` type. To elaborate, `UIElement`'s base classes are the following:
`Android.Views.View` → `Android.Views.ViewGroup` → `Uno.UI.UnoViewGroup` → `Uno.UI.Controls.BindableView` → `Windows.UI.Xaml.UIElement`

Recall that `UIElement` implements `DependencyObect` [as an interface](uno-internals-overview.md) in Uno.

## Layouting

Uno's measure and arrange logic is triggered from the native Android layout cycle. For a schematic of the control flow, see [Layouting in Android](xref:Uno.Contributing.LayoutingAndroid).
