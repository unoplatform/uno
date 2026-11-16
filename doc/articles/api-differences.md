---
uid: Uno.Development.ApiDifferences
---

# Differences between Uno.UI and WinUI

Uno Platform strives to closely replicate the WinUI API on all platforms and ensure that existing WinUI code is 100% compatible with Uno. This article covers areas where Uno.UI's implementation differs, typically to better integrate with the native platform, or where the capabilities of .NET differ due to inherent limitations of the native platform.

This article doesn't cover parts of the API that haven't been implemented yet. You can consult a [complete list of implemented and unimplemented controls here](implemented-views.md).

For a practical guide to addressing differences between Uno Platform and WinUI, [read this article](migrating-guidance.md).

## API differences

### The type hierarchy matches WinUI on every platform

As in WinUI, all visual elements inherit from `FrameworkElement`, which inherits from `UIElement`, which inherits from the `DependencyObject` **class**. That is true on every target as of Uno Platform 7.0.

Before 7.0, the Android, iOS and macOS renderers made `UIElement` inherit the native base view type for each platform (`ViewGroup`, `UIView`, `NSView`), which in turn forced `DependencyObject` to be an *interface* rather than a class — so a type inheriting directly from `DependencyObject` had to be declared `partial` for the generator to supply the implementation. Both differences are gone: 7.0 renders with Skia on every target, `DependencyObject` is an ordinary class, and no `partial` keyword is required.

See the [migration guide](xref:Uno.Development.MigratingToUno7) if you are upgrading code written against the older model.

## Runtime differences

### iOS is AOT-only

.NET code [must be Ahead-Of-Time (AOT) compiled to run on iOS](https://learn.microsoft.com/xamarin/ios/internals/limitations), as a fundamental platform limitation. As a result, a few APIs that require runtime code generation (eg `System.Reflection.Emit`) do not work. This includes code that uses the `dynamic` keyword.

### WebAssembly is single-threaded

Currently, WebAssembly code in the browser executes on a single thread. This limitation is expected to be lifted in the future, but for now, code that expects additional threads to be available may not function as expected.

[This GitHub issue](https://github.com/unoplatform/uno/issues/2302) tracks support for multi-threading on WebAssembly in Uno Platform.
