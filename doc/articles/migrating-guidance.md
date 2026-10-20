---
uid: Uno.Development.MigratingGuidance
---

# General guidance for making WinUI/UWP-only code Uno compatible

This article explains adjustments that may need to be made to WinUI/UWP-only code for it to run on Uno Platform, be it in an application or a class library.

## Code adjustments

### Add 'partial' to some class definitions

Certain class definitions will need to have the [`partial` keyword](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/partial-type) added, so that Uno's source generators can add code to them at compile time.

You'll need to do this for:

- types backed by a XAML file, which the XAML generator completes
- types declaring a `[GeneratedDependencyProperty]`

> [!NOTE]
> Inheriting directly from `DependencyObject` no longer requires `partial`. Before Uno Platform 7.0, `DependencyObject` was an interface whose implementation a source generator supplied; it is now an ordinary class. See [`DependencyObject` is a class](./uno-development/uno-internals-overview.md#dependencyobject-is-a-class).

Apart from adding `partial`, you don't need to worry about the generated code. You may however get misleading errors from Intellisense until the first time you try to compile the project, because the generated partial classes haven't been added yet.

> [!NOTE]
> Since Uno Platform 7.0 the UI is rendered with Skia on every target, and `FrameworkElement` no longer inherits a native Android or iOS view type. Earlier guidance about name collisions with native view members (such as `UIView.Window` or `View.TextAlignment`), and about nesting `FrameworkElement`-derived classes on iOS, no longer applies. To host a native view alongside Uno content, see [Incorporating native views](xref:Uno.Development.NativeViews).

## Adjust for unsupported runtime features

Not all .NET runtime features are supported on every platform. See [Migrating - Before You Start](migrating-before-you-start.md) for more details.
