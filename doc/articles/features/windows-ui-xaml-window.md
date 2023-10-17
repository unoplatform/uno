---
uid: Uno.Features.WinUIWindow
---

# Windows.UI.Xaml.Window

## Setting the background color for the Window

WinUI and UWP does not support the ability to provide a background color for `Window`, but Uno provides such an API through:

```
#if HAS_UNO
Uno.UI.Xaml.WindowHelper.SetBackground(Window.Current, new SolidColorBrush(Colors.Red));
#endif
```

This feature can help blend the background of the window with the background of the rendered app content when the window is resized.

### Supported platforms

|                            | Skia+GTK | Skia+WPF | iOS   | Android | macOS | Catalyst | WebAssembly |
| -------------------------- | :------: | :------: | :---: | :-----: | :---: | :------: | :---------: |
| `WindowHelper.SetBackground` |   ✔️     |    ✔️    | ❌    |  ❌     |  ❌  |   ❌     |  ❌         |

## Retrieving the native window

On some platforms, the window is backed by a native type. For those cases, you can use the `GetNativeWindow` helper:

```
#if HAS_UNO
var nativeWindow = Uno.UI.Xaml.WindowHelper.GetNativeWindow(Window.Current);
```

The `nativeWindow` is an object, so you need to cast it to the specific type on the given platform. See the table below:

|                            | Skia+GTK | Skia+WPF | iOS   | Android | macOS | Catalyst | WebAssembly |
| -------------------------- | :------: | :------: | :---: | :-----: | :---: | :------: | :---------: |
| `WindowHelper.GetNativeWindow`` | `Gtk.Window` |  `System.Windows.Window`    | `UIKit.UIWindow`    |  `Android.App.Activity`     |  `AppKit.NSWindow`  |   `UIKit.UIWindow`     |  `null`         |