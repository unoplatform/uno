---
uid: Uno.Development.ApiDifferences
---

# Differences between Uno.UI and UWP/WinUI

Uno Platform strives to closely replicate the UWP/WinUI API on all platforms and ensure that existing WinUI code is 100% compatible with Uno. This article covers areas where Uno.UI's implementation differs, typically to better integrate with the native platform, or where the capabilities of .NET differ due to inherent limitations of the native platform.

This article doesn't cover parts of the API which haven't been implemented yet. You can consult a [complete list of implemented and unimplemented controls here](implemented-views.md).

For a practical guide to addressing differences between Uno Platform and WinUI, [read this article](migrating-guidance.md).

## API differences

### `FrameworkElement` inherits from native base view types (Android, iOS, macOS)

As for WinUI, all visual elements in Uno.UI inherit from `FrameworkElement`, which inherits from `UIElement`. (At least, those that are publicly available.) On Windows, `UIElement` inherits from the `DependencyObject` class, which inherits from `System.Object`.

On Android, iOS, and macOS, `UIElement` instead inherits from the native base view type for each platform, as exposed to .NET by Xamarin Native. So, `ViewGroup` for Android, `UIView` for iOS, and `NSView` for macOS.

This allows native views (not defined by Uno.UI or inheriting from `FrameworkElement`) to be directly integrated into the visual tree, [in XAML markup or C# code](native-views.md).

### `DependencyObject` type is an interface (all non-Windows platforms)

This API difference follows directly from the previous one. In order to support native view inheritance, Uno.UI defines `DependencyObject` as an interface, rather than a class.

This is as transparent as possible to the application developer. For example, if a developer defines a class that inherits directly from `DependencyObject`, Uno.UI will automatically generate code that implements the `DependencyObject` interface methods. The only developer action required is to add the `partial` keyword to the class definition.

## Runtime differences

### iOS is AOT-only

.NET code [must be Ahead-Of-Time (AOT) compiled to run on iOS](https://docs.microsoft.com/en-us/xamarin/ios/internals/limitations), as a fundamental platform limitation. As a result, a few APIs that require runtime code generation (eg `System.Reflection.Emit`) do not work. This includes code that uses the `dynamic` keyword.

### WebAssembly is single-threaded

Currently, WebAssembly code in the browser executes on a single thread. This limitation is expected to be lifted in the future, but for now, code that expects additional threads to be available may not function as expected.

[This GitHub issue](https://github.com/unoplatform/uno/issues/2302) tracks support for multi-threading on WebAssembly in Uno Platform.
