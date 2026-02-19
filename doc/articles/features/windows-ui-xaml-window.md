---
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

## Fixing a window size and preventing resizing

To set a fixed size for the main window on the desktop target, configure the `OverlappedPresenter` in the `OnLaunched` method of App.Xaml.cs. This change will prevent users from resizing the window.

```csharp
protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
    //...
    const int targetWidth = 2048;
    const int targetHeight = 1536;

    MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = targetWidth, Height = targetHeight });

    // Disable resizing
    if (MainWindow.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
    {
        presenter.IsResizable = false; // Disable window resizing
        presenter.IsMaximizable = false; // Disable window maximizing
        presenter.PreferredMinimumWidth = targetWidth; // Set minimum width
        presenter.PreferredMinimumHeight = targetHeight; // Set minimum height
    }
}
```

## Allowing window size within specific limits

You can set maximum dimensions using `PreferredMaximumWidth` and `PreferredMaximumHeight`:

```csharp
if (MainWindow.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
{
    presenter.PreferredMaximumWidth = 2560; // Set maximum width
    presenter.PreferredMaximumHeight = 1440; // Set maximum height
}
```

These properties allow you to set the maximum dimensions for the window. When set, the window cannot be resized beyond these values by the user. This is useful for constraining the window size to fit specific design requirements or to prevent the window from becoming too large for the content to display properly.

> [!NOTE]
> If your app uses Uno Navigation, set the sizing options before you start the Host (see the code below). This makes sure the window appears as expected.

```csharp
// Set the window size here
Host = await builder.NavigateAsync<Shell>();
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

### Checking if title bar customization is supported

Before customizing the title bar, you should always check if title bar customization is available on the current platform:

```csharp
if (AppWindowTitleBar.IsCustomizationSupported())
{
    // Customize the title bar
    myWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
}
```

Currently, `IsCustomizationSupported()` returns `true` on Desktop Windows (net10.0-desktop) and WinAppSDK (net10.0-windows10.0.x) targets.

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
> This API is currently supported on Desktop Windows (net10.0-desktop) and WinAppSDK (net10.0-windows10.0.x) targets. On other platforms, the method exists but may have limited or no effect.

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
> This API is currently supported on Desktop Windows (net10.0-desktop) and WinAppSDK (net10.0-windows10.0.x) targets. On other platforms, the property exists but may have limited or no effect.

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

The actual height in pixels (not scaled points) is calculated based on the DPI scaling of the window and can be retrieved from the `Height` property:

```csharp
int titleBarHeight = myWindow.AppWindow.TitleBar.Height; // Height in actual pixels
```

> [!NOTE]
> `PreferredHeightOption` is supported on Desktop Windows (net10.0-desktop) and WinAppSDK (net10.0-windows10.0.x) targets when the title bar is extended into the content area. The `Height` property returns the value in actual pixels, not scaled points.

### Setting drag rectangles

The `AppWindowTitleBar.SetDragRectangles` method allows you to define specific rectangular regions in the title bar area that the user can drag to move the window:

```csharp
var dragRectangles = new[]
{
    new RectInt32(10, 0, 200, 32),  // Draggable region from x=10 to x=210
    new RectInt32(250, 0, 300, 32)  // Another draggable region from x=250 to x=550
};

myWindow.AppWindow.TitleBar.SetDragRectangles(dragRectangles);
```

> [!IMPORTANT]
>
> * The rectangles are specified in actual pixels, not scaled points.
> * Drag rectangles must be updated when the window size or DPI scaling changes, otherwise they will be incorrectly positioned.
> * This API is supported on Desktop Windows (net10.0-desktop) and WinAppSDK (net10.0-windows10.0.x) targets.

Example of updating drag rectangles on window size changes:

```csharp
myWindow.SizeChanged += (sender, args) =>
{
    // Recalculate and update drag rectangles based on new window size
    UpdateDragRectangles();
};
```

### Advanced: Setting non-client pointer source regions

The `InputNonClientPointerSource.SetRegionRects` method provides advanced control over non-client regions, which is particularly useful when you use `SetBorderAndTitleBar(true, false)` and render your own caption buttons (minimize, maximize, close).

By setting the region rectangles for caption buttons, you enable Windows features like the Snap Layouts popup that appears when hovering over the maximize button:

![Snap Layouts popup](https://github.com/user-attachments/assets/bade7d00-1559-4022-a8c7-820d42e4ed74)

```csharp
using Microsoft.UI.Input;

var appWindow = myWindow.AppWindow;
var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(appWindow.Id);

// Define the maximize button region to enable Snap Layouts
var maximizeButtonRect = new RectInt32(100, 0, 46, 32); // x, y, width, height in actual pixels

nonClientInputSrc.SetRegionRects(NonClientRegionKind.Caption, new[] { maximizeButtonRect });
```

> [!IMPORTANT]
>
> * Region rectangles are specified in actual pixels, not scaled points.
> * These rectangles must be updated when the window size or DPI scaling changes.
> * This API is supported on Desktop Windows (net10.0-desktop) and WinAppSDK (net10.0-windows10.0.x) targets.

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
