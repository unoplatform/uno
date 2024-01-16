---
uid: Uno.Features.Clipboard
---

# Clipboard

> [!TIP]
> This article covers Uno-specific information for `Clipboard`. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/windows/uwp/app-to-app/copy-and-paste

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

* `SetContent` and `GetContent` APIs currently support textual data on all platforms. On Android, they also support URI and HTML formats, but the clipboard can hold only one item. Setting multiple items at once does not work reliably.
* `ContentChanged` event can observe clipboard changes only when the application is in the foreground. On macOS, the `ContentChanged` event checks for clipboard changes by polling the current `NSPasteboard` change count in 1-second intervals. The polling starts only after the first subscriber attaches to the `ContentChanged` event and stops after the last subscriber unsubscribes.
* `Flush` operation has an empty implementation. In contrast to UWP, on other platforms, data automatically remain in the clipboard even after the application is closed.

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
