---
uid: Uno.Development.NativeViews
---

# Incorporating native views to the Uno visual tree

The Android, iOS, and macOS targets for Uno support the notion of a purely native view, that is not coming from Uno Platform but instead defined in a third-party library, via a Xamarin binding, or in the native framework itself. Since Uno's views are inheriting from the base native view type on these platforms, you can incorporate native views into your app's visual tree.

## Adding JavaScript views in WebAssembly

On WebAssembly, for both Native and Skia renderers, [Read this guide](xref:Uno.Interop.WasmJavaScript1) to learn how to use native views.

## Adding native views in Skia

On Skia targets, integrating native views is done differently. [Read this guide](xref:Uno.Skia.Embedding.Native) to learn how.

## Native Views for Native iOS/Android

### Adding native views in XAML

There's no special syntax required when adding native views in XAML when using the Native renderer, apart from [platform conditionals](xref:Uno.Development.PlatformSpecificXaml) to ensure that the XAML compiles for all platforms. Uno's XAML parser supplies the needed 'glue', including supplying common constructor parameters (such as the `Context` parameter on Android).

An example:

```xml
Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"
xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
xmlns:android="http://uno.ui/android"
xmlns:androidwidget="using:Android.Widget"
mc:Ignorable="d android"
...

<StackPanel Margin="0,30,0,0">
 <TextBlock Text="Rating" />
 <android:Grid Background="Beige"
      Width="200">
  <androidwidget:RatingBar />
 </android:Grid>
</StackPanel>
```

### Adding native views in code

Adding native views in C# code requires you to first 'wrap' the native view in a special `UIElement`, because 'container' elements like `Panel` and `Border` expect a child of type `UIElement` (as of Uno 3.0 and above). The recommended way to do this is with the `VisualTreeHelper.AdaptNative()` static method:

```csharp
using Windows.UI.Xaml.Media
...

var ratingBar = new Android.Widget.RatingBar(Uno.UI.ContextHelper.Current);
var wrapped = VisualTreeHelper.AdaptNative(ratingBar);
_myBorder.Child = wrapped;
```

Note that `VisualTreeHelper.AdaptNative()` will throw an exception if it receives a `FrameworkElement`. If you're in a context where you don't know if the view you want to display is actually a purely native view or a managed `FrameworkElement` type, you can use `VisualTreeHelper.TryAdaptNative()` instead.

Assigning a native view directly as the `Content` property of `ContentPresenter` or `ContentControl` is also supported (since `Content` is of type `object`).

### Troubleshooting

Uno makes certain assumptions about native views when it displays them, which may not always hold (eg that `SizeThatFits()` is implemented on iOS). Here are some things to try if your view isn't displaying:

- check the documentation and ensure you're configuring the native control correctly.
- try setting a fixed `Width` and `Height` on the outer XAML container.
- try setting the dimensions of the native view via code-behind.
