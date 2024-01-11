---
uid: Uno.Features.WinUIWindow
---

# Window

> [!TIP]
> This article covers Uno-specific information for `Microsoft.UI.Xaml.Window`. For a full description of the feature and instructions on using it, consult the Microsoft documentation: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window

* The `Microsoft.UI.Xaml.Window` class allows for managing the window of the application.

## Setting the background color for the Window

WinUI and UWP does not support the ability to provide a background color for `Window`, but Uno provides such an API through:

```csharp
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

```csharp
#if HAS_UNO
var nativeWindow = Uno.UI.Xaml.WindowHelper.GetNativeWindow(MainWindow);
#endif
```

The `nativeWindow` is an `object`, so you need to cast it to the specific type on the given platform. See the table below:

|                            | Skia+GTK | Skia+WPF | iOS   | Android | macOS | Catalyst | WebAssembly |
| -------------------------- | :------: | :------: | :---: | :-----: | :---: | :------: | :---------: |
| `WindowHelper.GetNativeWindow`` |`Gtk.Window`|`System.Windows.Window`|`UIKit.UIWindow`|`Android.View.Window`|`AppKit.NSWindow`|`UIKit.UIWindow`|`null`         |
