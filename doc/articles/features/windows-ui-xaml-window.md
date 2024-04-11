---
uid: Uno.Features.WinUIWindow
---

# Windowing

> [!TIP]
> This article covers Uno-specific information for `Microsoft.UI.Xaml.Window` and windowing. For a full description of the feature and instructions on using it, see [Window Class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window) and [Windowing Overview](https://learn.microsoft.com/windows/apps/windows-app-sdk/windowing/windowing-overview).

* The `Microsoft.UI.Xaml.Window` class allows for managing the window of the application.
* This article also describes window management in Uno Platform apps

## Explaining basic Windowing APIs

There are several "window" types in Uno Platform apps, some are legacy only and will be removed in future releases.

* `Microsoft.UI.Xaml.Window` - the main type representing a XAML-based Window. Allows for basic manipulation and setting content.
* `Microsoft.UI.Windowing.AppWindow` - represents a general Window (may not be XAML based in case of WinUI target). Includes advanced capabilities not present on `Window` itself.

The legacy types are:

* `Windows.UI.Core.CoreWindow`
* `Windows.UI.WindowManagement.AppWindow`

While these are still available for backward compatibility purposes, they will be deprecated and no longer retrievable in future releases of Uno Platform.

## Creating a new window

To display a new window (both primary and secondary), you can utilize the `Microsoft.UI.Xaml.Window` constructor:

```csharp
var window = new Window();
window.Content = new TextBlock() { "Hello, world" };
window.Activate();
```

### Support for secondary windows

All Uno Platform desktop targets support displaying secondary windows. Attempt to create a second window on Android and iOS will result in a `InvalidOperation` exception. Support for multi-windowing in mobile targets is actively investigated.

## Making the window full screen

`AppWindow` provides various ways to manipulate the display of a window including the ability to make it run in full-screen mode. Use the `SetPresenter` method to switch to this mode:

```csharp
myWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen); 
```

To exit full-screen mode, switch back to default:

```csharp
myWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
```

## Minimizing or maximizing a window

Use the `OverlappedPresenter` to minimize or maximize your window in desktop environments. The `Overlapped` presenter is set by default, unless you switch to full screen (see above).

```csharp
var overlappedPresenter = (OverlappedPresenter)myWindow.AppWindow.Presenter;
// Minimize the window
overlappedPresenter.Minimize();

// Restore window
overlappedPresenter.Restore();

// Maximize the window
overlappedPresenter.Maximize();
```

## Enumerating windows

WinUI currently does not provide a way to enumerate open windows of an application. Due to this limitation we recommend you to track the windows manually.

To simplify this on non-WinUI targets, we provide the `ApplicationHelper.Windows` property to enumerate currently available windows:

```csharp
#if HAS_UNO
foreach (var window in ApplicationHelper.Windows)
{
    // Do something.
}
#endif
```

**Note:** Make sure to access this property from the UI thread.

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

## Avoiding `Window.Current`

`Window` type has a static `Current` property which previously served as a way to access the main, singleton window instance. This is no longer relevant in multi-window scenarios. For legacy reasons and to maintain compatibility with existing Uno Platform apps, we opted to keep this property and it returns the first window that was created. You should not rely on this behavior going forward as it will be deprecated

## Setting the background color for the Window

WinUI and UWP does not support the ability to provide a background color for `Window`, but Uno provides such an API through:

```csharp
#if HAS_UNO
Uno.UI.Xaml.WindowHelper.SetBackground(Window.Current, new SolidColorBrush(Colors.Red));
#endif
```

This feature can help blend the background of the window with the background of the rendered app content when the window is resized.

### Supported platforms

|                            | Skia Desktop | iOS   | Android | macOS | Catalyst | WebAssembly |
| -------------------------- | :------: | :---: | :-----: | :---: | :------: | :---------: |
| `WindowHelper.SetBackground` |   ✔️     | ❌    |  ❌     |  ❌  |   ❌     |  ❌         |
