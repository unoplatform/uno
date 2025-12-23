---
uid: Uno.Development.MigratingGuidance
---

# General guidance for making WinUI/UWP-only code Uno compatible

This article explains adjustments that may need to be made to WinUI/UWP-only code for it to run on Uno Platform, be it in an application or a class library.

## Code adjustments

### Add 'partial' to some class definitions

Certain class definitions will need to have the [`partial` keyword](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/partial-type) added. This is because Uno [generates additional code at compile-time](./uno-development/uno-internals-overview.md#dependencyobject-implementation-generator) to support them properly.

You'll need to do this for:

- types that inherit from `FrameworkElement`, directly or indirectly
- types that inherit directly from `DependencyObject`

Apart from adding `partial`, you don't need to worry about the generated code. You may however get misleading errors from Intellisense until the first time you try to compile the project, because the generated partial classes haven't been added yet.

### Avoid nested classes that inherit from FrameworkElement

Classes that inherit from `FrameworkElement` directly or indirectly can't be nested inside another class; it's been observed to cause problems on Xamarin.iOS. If you have any nested `FrameworkElement`-derived classes, you'll need to refactor them to be top-level classes.

### Disambiguate naming collisions with native properties

This is relevant if you're targeting Android, iOS, and/or macOS, where Uno views (hence, `FrameworkElement`) [inherit from the native view type](native-views.md). In some cases you may find that a reference to a type from the UWP framework is confused with a native property. The fix in this case is generally to disambiguate by supplying the full namespace of the type.

Some common cases:

- on iOS, inside a control definition, references to [`Window.Current`](https://learn.microsoft.com/uwp/api/windows.ui.xaml.window.current) will be confused with the [`UIView.Window`](https://learn.microsoft.com/dotnet/api/uikit.uiview.window) property. The fix is to fully qualify this as `Windows.UI.Xaml.Current`.
- on Android, inside a control definition, references to the [`TextAlignment` enum](https://learn.microsoft.com/uwp/api/windows.ui.xaml.textalignment) will be confused with the [`View.TextAlignment` property](https://learn.microsoft.com/dotnet/api/android.views.view.textalignment). The fix, again, is to fully qualify the reference as `Windows.UI.Xaml.TextAlignment`.

#### What do I do if I have a nested namespace with `Windows` in it?

If, for example, your control is defined in the `CoolControls` namespace, and you've also defined a `CoolControls.Windows` namespace, then the above will give a compilation error. You'll need to use the [`global` keyword](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/namespace-alias-qualifier), eg `global::Windows.UI.Xaml.Window.Current`.

## Adjust for unsupported runtime features

Not all .NET runtime features are supported on every platform. See [Migrating - Before You Start](migrating-before-you-start.md) for more details.
