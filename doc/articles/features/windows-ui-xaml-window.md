﻿---
uid: Uno.Features.WinUIWindow
---

# Windowing

> [!TIP]
> This article covers Uno-specific information for `Microsoft.UI.Xaml.Window` and windowing. For a full description of the feature and instructions on using it, see [Window Class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window) and [Windowing Overview](https://learn.microsoft.com/windows/apps/windows-app-sdk/windowing/windowing-overview).
>
> [!Video https://www.youtube-nocookie.com/embed?listType=playlist&list=PLl_OlDcUya9qHAzFlO5Z6SGJ8zHg-nYkh]
For more information and detailed walkthroughs on Windowing, please refer to the rest of the [video playlist](https://www.youtube.com/playlist?list=PLl_OlDcUya9qHAzFlO5Z6SGJ8zHg-nYkh).

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

WinUI currently does not provide a way to enumerate open windows of an application. Due to this limitation, we recommend you to track the windows manually.

To simplify this on non-WinUI targets, we provide the `ApplicationHelper.Windows` property to enumerate currently available windows:

```csharp
#if HAS_UNO
foreach (var window in ApplicationHelper.Windows)
{
    // Do something.
}
#endif
```

> [!NOTE]
> Make sure to access this property from the UI thread.

## Retrieving the native window

On some platforms, the window is backed by a native type. For those cases, you can use the `GetNativeWindow` helper:

```csharp
#if HAS_UNO
var nativeWindow = Uno.UI.Xaml.WindowHelper.GetNativeWindow(MainWindow);
#endif
```

The `nativeWindow` is an `object`, so you need to cast it to the specific type on the given platform. See the table below:

|                                |   Skia+GTK   |               Skia+X11                |        Skia+WPF         |       iOS        |        Android        |       macOS       | WebAssembly |
| ------------------------------ | :----------: | :-----------------------------------: | :---------------------: | :--------------: | :-------------------: | :---------------: | :---------: |
| `WindowHelper.GetNativeWindow` | `Gtk.Window` | `Uno.UI.Runtime.Skia.X11NativeWindow` | `System.Windows.Window` | `UIKit.UIWindow` | `Android.View.Window` | `AppKit.NSWindow` |   `null`    |

## Avoiding `Window.Current`

`Window` type has a static `Current` property which previously served as a way to access the main, singleton window instance. This is no longer relevant in multi-window scenarios. For legacy reasons and to maintain compatibility with existing Uno Platform apps, we opted to keep this property and it returns the first window that was created. You should not rely on this behavior going forward as it will be deprecated.

## Customizing window border and title bar

### Setting border and title bar visibility

The `OverlappedPresenter.SetBorderAndTitleBar` method allows you to control whether the window displays a border and title bar:

```csharp
var overlappedPresenter = (OverlappedPresenter)myWindow.AppWindow.Presenter;
// Remove both border and title bar
overlappedPresenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);

// Show border but hide title bar
overlappedPresenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);

// Show both border and title bar
overlappedPresenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: true);
```

> [!NOTE]
> This API is currently supported on Desktop Windows (net9.0-desktop, net10.0-desktop) and WinAppSDK (net9.0-windows10.0.x, net10.0-windows10.0.x) targets. On other platforms, the method exists but may have limited or no effect.

### Extending content into title bar

The `AppWindowTitleBar.ExtendsContentIntoTitleBar` property allows you to extend your app content into the title bar area, giving you full control over the title bar region:

```csharp
myWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
```

When you extend content into the title bar, you typically want to provide a custom title bar element using `Window.SetTitleBar`:

```csharp
myWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
myWindow.SetTitleBar(myCustomTitleBarElement);
```

> [!NOTE]
> This API is currently supported on Desktop Windows (net9.0-desktop, net10.0-desktop) and WinAppSDK (net9.0-windows10.0.x, net10.0-windows10.0.x) targets. On other platforms, the property exists but may have limited or no effect.

### Configuring title bar height

When extending content into the title bar, you can control the height of the title bar using the `PreferredHeightOption` property:

```csharp
// Standard height (32px at default DPI)
myWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

// Tall height (48px at default DPI)
myWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

// Collapsed (0px)
myWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
```

The actual height in pixels is calculated based on the DPI scaling of the window and can be retrieved from the `Height` property:

```csharp
int titleBarHeight = myWindow.AppWindow.TitleBar.Height;
```

> [!NOTE]
> `PreferredHeightOption` is supported on Desktop Windows (net9.0-desktop, net10.0-desktop) and WinAppSDK (net9.0-windows10.0.x, net10.0-windows10.0.x) targets when the title bar is extended into the content area.

### Checking if title bar customization is supported

You can check if title bar customization is available on the current platform using:

```csharp
if (AppWindowTitleBar.IsCustomizationSupported())
{
    // Customize the title bar
    myWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
}
```

Currently, `IsCustomizationSupported()` returns `true` on Desktop Windows (net9.0-desktop, net10.0-desktop) and WinAppSDK (net9.0-windows10.0.x, net10.0-windows10.0.x) targets.

## Setting the background color for the Window

WinUI and UWP does not support the ability to provide a background color for `Window`, but Uno provides such an API through:

```csharp
#if HAS_UNO
Uno.UI.Xaml.WindowHelper.SetBackground(Window.Current, new SolidColorBrush(Colors.Red));
#endif
```

This feature can help blend the background of the window with the background of the rendered app content when the window is resized.

### Supported platforms

|                              | Skia Desktop | iOS | Android | macOS | WebAssembly |
| ---------------------------- | :----------: | :-: | :-----: | :---: | :---------: |
| `WindowHelper.SetBackground` |      ✔️     | ❌  |   ❌    |  ❌  |     ❌     |
