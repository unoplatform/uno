---
uid: Uno.Features.AppCloseHandler
---

# Preventing Window Closing

> [!TIP]
> This article covers Uno Platform–specific behavior. For the full API documentation, see [AppWindow.Closing Event](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.closing).

The `AppWindow.Closing` API lets you respond to or prevent standard app window closing requests—such as clicking the window’s close button or pressing <kbd>Alt</kbd>+<kbd>F4</kbd>. Note that this does **not** block the user from terminating the app by force (e.g., via Task Manager or `kill`).

## Platform Support

| Feature              | Windows App SDK | Android | iOS | Web (WASM) | Desktop (Windows) | Desktop (macOS) | Desktop (Linux) |
|----------------------|------------------|---------|-----|------------|-------------------|------------------|------------------|
| `AppWindow.Closing`  | ✔️               | ❌      | ❌  | ❌         | ✔️                | ✔️               | ✔️               |

> [!NOTE]
> On platforms where this feature is not supported, the `Closing` event will still be raised, but setting `args.Cancel = true` has no effect.

## Usage Example

```csharp
MyWindow.AppWindow.Closing += OnAppWindowClosing;

private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
{
    // Replace with your own logic, such as checking for unsaved changes
    bool cancelClose = ShouldWindowStayOpen();

    // Cancel the close request if needed
    args.Cancel = cancelClose;
}
```

> [!IMPORTANT]
> The `AppWindow.Closing` event must be handled synchronously. Asynchronous operations (e.g., showing a `ContentDialog`) are not allowed and will not delay the closing process.

## Quitting the App on macOS

On macOS, quitting the app with <kbd>Cmd</kbd>+<kbd>Q</kbd>, the application menu's **Quit** or the Dock's **Quit** closes its windows as the user would:

- Each window the user can see raises `AppWindow.Closing`, front to back, and then `Window.Closed`.
- If a handler sets `args.Cancel = true`, the quit is cancelled. The windows asked before it are closed; that window and the ones behind it stay open.
- Once the windows are closed, the app exits, whatever `Application.DispatcherShutdownMode` is. `OnExplicitShutdown` keeps the app running only after its last window is closed, and a later quit still ends it.
- The quit is refused while a file picker or a sheet is open.

A logout, restart or shutdown ends the app without raising `AppWindow.Closing` or `Window.Closed`, as on Windows.

As on WinUI, `Application.Current.Exit()` closes every window as `Window.Close()` does, raising `Window.Closed` but not `AppWindow.Closing`, and then ends the app. A `Window.Closed` handler that sets `args.Handled = true` keeps its window open, and with `OnLastWindowClose` that window keeps the app running.

> [!NOTE]
> `AppWindow.Closing` does not tell a quit from a click on the window's close button. An app that cancels `Closing` to hide its window instead of closing it also cancels the quit, so it needs its own way to exit, such as a menu item that calls `Application.Current.Exit()`.
