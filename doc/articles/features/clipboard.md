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

Browsers place security restrictions on clipboard access, so keep the following in mind:

* Copying supports text, HTML, and bitmap formats. Bitmaps are converted to PNG, since that is the only image format browsers accept. Custom formats set with `DataPackage.SetData` are written as [web custom formats](https://developer.chrome.com/blog/web-custom-formats-for-the-async-clipboard-api) when the format id is a valid MIME type (for example `application/x-myapp`), the value is a string, and the browser supports them (currently Chromium-based browsers only). On other browsers the custom format is left off the system clipboard, though your own app can still read it back.
* Trigger copies from a user interaction such as a button click. Outside a user gesture the browser rejects the write, and while your app can still read the content back, other applications will not see it.
* Reading back content your own app copied always works and never prompts the user.
* To support pasting, read the clipboard in response to a paste gesture such as <kbd>Ctrl</kbd>+<kbd>V</kbd> (for example with a `KeyboardAccelerator`). This never prompts the user, and everything on the clipboard is available, including text, HTML, images (through `GetBitmapAsync`), and files copied from the OS file manager (through `GetStorageItemsAsync`). Large files are streamed as you read them, so they can be pasted cheaply.
* Reading the clipboard at any other time, such as from a menu action, may show the browser's permission prompt, and only text, HTML, and PNG images are available. Browsers never expose copied files outside a paste gesture. If the user denies access, the `DataPackageView.Get*Async` methods throw an `InvalidOperationException` whose `InnerException` is an `UnauthorizedAccessException`, so you can tell denial apart from an empty clipboard.
* `Clear` cannot truly empty the OS clipboard; it writes an empty text entry instead.
* `ContentChanged` is raised for your own writes, for copy/cut/paste within the page, and when the window regains focus. Changes made in other applications cannot be observed while your app is in the background.

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
