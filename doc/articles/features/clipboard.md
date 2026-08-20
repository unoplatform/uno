---
uid: Uno.Features.Clipboard
---

# Clipboard

> [!TIP]
> This article covers Uno-specific information for `Clipboard`. For a full description of the feature and instructions on using it, see [Copy and paste](https://learn.microsoft.com/windows/uwp/app-to-app/copy-and-paste).

* The `Windows.ApplicationModel.DataTransfer.Clipboard` class allows you to copy content from your application, and paste the content into your application.

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|------------------|---------|---------|-----|------------|-------|--------------|--------------|
| `SetContent`     | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |
| `GetContent`     | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |
| `Clear`          | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |
| `ContentChanged` | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |
| `Flush`          | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |

<!-- Add any additional information on platform-specific limitations and constraints -->

## Using Clipboard with Uno

> [!Video https://www.youtube-nocookie.com/embed/bfT4_LZrSQQ]

* `SetContent` and `GetContent` APIs currently support textual data on all platforms. On Android, they also support URI and HTML formats, but the clipboard can hold only one item. Setting multiple items at once does not work reliably.
* `ContentChanged` event can observe clipboard changes only when the application is in the foreground. On macOS, the `ContentChanged` event checks for clipboard changes by polling the current `NSPasteboard` change count in 1-second intervals. The polling starts only after the first subscriber attaches to the `ContentChanged` event and stops after the last subscriber unsubscribes.
* `Flush` operation has an empty implementation. In contrast to WinUI, on other platforms, data automatically remains in the clipboard even after the application is closed.

### Web (WASM) specifics

On the browser, the clipboard is backed by the [async Clipboard API](https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API) and the DOM `paste` event, which imposes browser security rules on some operations:

* **Writing** supports text, HTML, and bitmap formats, written atomically as a single clipboard item. Non-PNG bitmaps are transcoded to PNG, as browsers only accept `image/png` clipboard writes. Custom formats set through `DataPackage.SetData` are written as [web custom formats](https://developer.chrome.com/blog/web-custom-formats-for-the-async-clipboard-api) when the browser supports them (the format id must be a valid MIME type, e.g. `application/x-myapp`) and the value is a string. Writes should happen in response to a user interaction; browsers may reject writes outside a user gesture.
* **Reading content this application wrote** requires no permission — the content is served from an internal cache that stays authoritative until the clipboard may have changed (window focus change, or an in-page `copy`/`cut`).
* **Reading during a paste gesture** (<kbd>Ctrl</kbd>+<kbd>V</kbd>) requires no permission. When the user pastes, Uno captures the browser `paste` event, and a `GetContent()` call made in response (e.g. from a `KeyboardAccelerator`) sees exactly the pasted formats — including **files copied from the OS file manager**, exposed through `DataPackageView.GetStorageItemsAsync()`, and pasted images through `GetBitmapAsync()`. File and image content is streamed on demand rather than copied eagerly, so pasting large files is inexpensive until the content is actually read.
* **Reading outside a paste gesture** (e.g. from a menu action) uses `navigator.clipboard.read()`, which prompts the user for permission and only exposes text, HTML, and PNG images — browsers never expose arbitrary files this way. When the read is denied, the `DataPackageView.Get*Async` methods fail with an inner `UnauthorizedAccessException`, allowing the application to distinguish denial from an empty clipboard.
* `Contains` and `AvailableFormats` report actual formats when they are knowable (after a paste gesture or an own write); otherwise text, HTML, and bitmap are advertised optimistically since the browser cannot enumerate clipboard formats without a permission prompt.
* `Clear` cannot truly empty the OS clipboard (browsers do not allow it); it writes an empty text entry and reports an empty clipboard to the application.
* `ContentChanged` is raised for own writes, in-page `copy`/`cut`/`paste`, and when the window regains focus (since external changes cannot be observed while the application is in the background).

## Examples

### Copying text to clipboard

```csharp
var dataPackage = new DataPackage();
dataPackage.SetText("Hello, clipboard");
Clipboard.SetContent(dataPackage);
```

### Pasting text from the clipboard

```csharp
var content = Clipboard.GetContent();
var text = await content.GetTextAsync();
```

### Observing clipboard changes

```csharp
Clipboard.ContentChanged += Clipboard_ContentChanged;

private void Clipboard_ContentChanged(object sender, object e)
{
    // ...
}
```
