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
